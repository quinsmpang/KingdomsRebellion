using System;
using System.IO;
using KingdomsRebellion.Core.Math;

namespace KingdomsRebellion.Network.Link {

	/// <summary>
	/// Action send over the network for select units.
	/// </summary>
	public class SelectAction : GameAction {

		event Action<int, Vec2> OnModelSelect;

		protected Vec2 _modelPoint;

		public static SelectAction FromBytes(byte[] data) {
			return new SelectAction().GetFromBytes(data) as SelectAction;
		}
	
		public SelectAction(Vec2 modelPoint) {
			_modelPoint = modelPoint;
		}

		protected SelectAction() {}

		public override void Process(int playerID) {
			Offer("OnModelSelect");
			if (OnModelSelect != null) {
				OnModelSelect(playerID, _modelPoint);
			}
			Denial("OnModelSelect");
		}

		public override byte ActionType() {
			return (byte) GameActionEnum.SelectAction;
		}

		protected override void Serialize(BinaryWriter writer) {
			base.Serialize(writer);
			_modelPoint.Serialize(writer);
		}
	
		protected override void Deserialize(BinaryReader reader) {
			base.Deserialize(reader);
			_modelPoint = Vec2.Deserialize(reader);
		}

	}

}