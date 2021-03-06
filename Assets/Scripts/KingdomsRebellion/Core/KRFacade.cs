﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using KingdomsRebellion.AI;
using KingdomsRebellion.Core.FSM;
using KingdomsRebellion.Core.Map;
using KingdomsRebellion.Core.Math;
using KingdomsRebellion.Inputs;
using UnityEngine;
using Debug = UnityEngine.Debug;
using KingdomsRebellion.Core.Components;

namespace KingdomsRebellion.Core {
	public static class KRFacade {

		static readonly InputNetworkAdapter _InputNetworkAdapter;
		static readonly IMap<QuadTreeNode<KRTransform>,KRTransform> _Map;

#if !UNITY_EDITOR
		static IList<Vec2> __walkedNode = new List<Vec2>();
		static IList<Vec2> __walkedFind = new List<Vec2>();
#endif

		static KRFacade() {
			_InputNetworkAdapter = new InputNetworkAdapter();
			_Map = new QuadTree<KRTransform>(256, 256);

#if !UNITY_EDITOR
			EventConductor.On(typeof(KRFacade), "OnKRDrawGizmos");
#endif
		}

		public static IEnumerable<QuadTreeNode<KRTransform>> FindPath(Vec2 start, Vec2 target) {
			return Pathfinding<QuadTreeNode<KRTransform>>.FindPath(_Map.FindNode(start), _Map.FindNode(target))
				.Select(abstractNode => abstractNode.WrappedNode());
		}

		public static void UpdateGame() {
            foreach (KRTransform unit in _Map) {
                FiniteStateMachine fsm = unit.GetComponent<FiniteStateMachine>();
                if (fsm != null) {
                    fsm.UpdateGame();
                }
            }
		}

		public static GameObject Find(Vec2 v) {
			KRTransform u = _Map.Find(v);
			return (u == null) ? null : u.gameObject;
		}

		public static void Add(KRTransform t) { _Map.Add(t); }
		public static void Remove(KRTransform t) { _Map.Remove(t); }
		public static bool IsEmpty(Vec2 v) { return _Map.IsEmpty(v); }
		public static bool IsInBounds(Vec2 v) { return _Map.IsInBounds(v); }

		/// <summary>
		/// Find all gameObjects in the rect define by v1 and v2 and v3.
		/// </summary>
		public static IEnumerable<GameObject> Find(Vec2 v1, Vec2 v2, Vec2 v3) {
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			Vec2 v4 = v2 - (v3 - v1);

#if !UNITY_EDITOR
			__walkedNode.Clear();
			__walkedFind.Clear();
			__walkedFind.Add(v1);
			__walkedFind.Add(v2);
			__walkedFind.Add(v3);
			__walkedFind.Add(v4);
#endif

			// bottomLeft, bottomRight, topLeft, topRight
			Vec2[] vecs = { v1,v2,v3,v4 };
			Array.Sort<Vec2>(vecs);
			Vec2 bottomLeft = vecs[0], bottomRight = vecs[2], topLeft = vecs[1], topRight = vecs[3];

			// boundBottomLeft, boundBottomRight, boundTopLeft, boundTopRight
			IEnumerable<int> xs = vecs.Select(v => v.X);
			IEnumerable<int> ys = vecs.Select(v => v.Y);

			int minXS = xs.Min();
			int maxXS = xs.Max();
			int minYS = ys.Min();
			int maxYS = ys.Max();

			Func<Vec2,Vec2,IList<Vec2>> _getLine = delegate(Vec2 orig, Vec2 dest) {
				IList<Vec2> d = new List<Vec2>();
				d.Add(orig);
				Bresenham.Line(orig.X, orig.Y, dest.X, dest.Y, delegate(int x, int y) {
					d.Add(new Vec2(x, y));
					return true;
				});
				return d;
			};

			IList<Vec2> bltp = _getLine(bottomLeft, topLeft);
			IList<Vec2> tltr = _getLine(topLeft, topRight);
			IList<Vec2> trbr = _getLine(topRight, bottomRight);
			IList<Vec2> brbl = _getLine(bottomRight, bottomLeft);

			// FIXME
			Func<Vec2,bool> _in = delegate(Vec2 v) {
				int x = v.X, y = v.Y;
				int ax = bottomLeft.X, bx = topLeft.X, dx = bottomRight.X;
				int ay = bottomLeft.Y, by = topLeft.Y, dy = bottomRight.Y;
				int ex=bx-ax, ey=by-ay, fx=dx-ax, fy=dy-ay;
				
				if ((x-ax) * ex + (y-ay) * ey < 0) return false;
				if ((x-bx) * ex + (y-by) * ey > 0) return false;
				if ((x-ax) * fx + (y-ay) * fy < 0) return false;
				if ((x-dx) * fx + (y-dy) * fy > 0) return false;
				
				return true;
			};
			Func<Vec2,bool> _on = delegate(Vec2 v) {
				return bltp.Contains(v) || tltr.Contains(v) || trbr.Contains(v) || brbl.Contains(v);
			};

			List<GameObject> gos = new List<GameObject>();

			KRTransform u;
			for (int x = minXS; x < maxXS; ++x) {
				for (int y = minYS; y < maxYS; ++y) {
					Vec2 c = new Vec2(x,y);
					if (_in(c) || _on(c)) {
#if !UNITY_EDITOR
						__walkedNode.Add(c);
#endif
						u = _Map.Find(c);
						if (u != null) { gos.Add(u.gameObject); }
					}
				}
			}

			stopwatch.Stop();
			Debug.Log(String.Format("DragFind :: time elapsed: {0}", stopwatch.Elapsed));

			return gos;
		}

		public static IEnumerable<GameObject> Around(Vec2 v, int maxDist) {
			return _Map.ToList().Where(u => u.Pos.Dist(v) <= maxDist).Select(u => u.gameObject);
		}

	    public static Vec2 NearSquareAt(Vec2 v1, Vec2 v2, int dist) {
	        if (dist <= 0) return null;
	        Vec2 dir = v1 - v2;
	        Vec2 leftB, leftT;
	        Vec2 res;
	        if (dir.X > 0) {
	            res = v2 + new Vec2(dist, 0);
	            leftB = v2 + new Vec2(dist, dist);
	            leftT = v2 + new Vec2(dist, -dist);
	        } else if (dir.X < 0) {
                res = v2 + new Vec2(-dist, 0);
                leftB = v2 + new Vec2(-dist, -dist);
                leftT = v2 + new Vec2(-dist, dist);
	        } else {
	            if (dir.Y < 0) {
                    res = v2 + new Vec2(0, -dist);
                    leftB = v2 + new Vec2(-dist, dist);
                    leftT = v2 + new Vec2(dist, dist);
	            } else {
                    res = v2 + new Vec2(0, dist);
                    leftB = v2 + new Vec2(dist, -dist);
                    leftT = v2 + new Vec2(-dist, -dist);
	            }
	        }
	        if (_Map.IsEmpty(res)) return res;
	        Vec2 left = new Vec2(res.X, res.Y);
            Vec2 right = new Vec2(res.X, res.Y);
	        bool onBottom = false;
            bool onTop = false;
	        while (!_Map.IsEmpty(left) || !_Map.IsEmpty(right)) {
	            if (!onBottom) {
	                left -= Vec2.Right;
                    right += Vec2.Right;
                } else if (!onTop) {
                    left += Vec2.Up;
                    right += Vec2.Up;
                } else {
                    left += Vec2.Right;
                    right -= Vec2.Right;
                }
	            if (left == leftB) onBottom = true;
	            if (left == leftT) onTop = true;
	            if (left == right && !_Map.IsEmpty(left)) return null;
	        }
	        return _Map.IsEmpty(left) ? left : right;
	    }
#if !UNITY_EDITOR
		static void OnKRDrawGizmos() {
			_Map.Walk(GizmosDrawNode);

			Gizmos.color = Color.magenta;
			foreach (var x in __walkedNode) {
				Gizmos.DrawWireCube(x.ToVector3() + Vector3.one*.5f, Vector3.one);
			}

			Gizmos.color = Color.red;
			foreach (var x in __walkedFind) {
				Gizmos.DrawWireCube(x.ToVector3() + Vector3.one*.5f, Vector3.one);
			}
		}

		static void GizmosDrawNode(QuadTreeNode<KRTransform> n) {
			Gizmos.color = n.IsFree() ? Color.blue : Color.green;
			Vector3 p0 = n.BottomLeft.ToVector3(), p1 = n.TopRight.ToVector3();
			Gizmos.DrawLine(p0, p0 + new Vector3(n.Width-.2f, 0, .1f));
			Gizmos.DrawLine(p0, p0 + new Vector3(.1f, 0, n.Height-.2f));
			Gizmos.DrawLine(p1, p1 - new Vector3(n.Width-.2f, 0, .1f));
			Gizmos.DrawLine(p1, p1 - new Vector3(.1f, 0, n.Height-.2f));
		}
#endif
	}
}