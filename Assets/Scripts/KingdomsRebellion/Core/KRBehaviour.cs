﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Object = System.Object;

namespace KingdomsRebellion.Core {

	/// <summary>
	/// KRBehaviour inherits from MonoBehaviour and add some useful utilities
	/// in relation with KingdomsRebellion system and architecture.
	/// </summary>
	public class KRBehaviour : MonoBehaviour {

		Dictionary<String, Object> _properties;

		protected KRBehaviour() {
			_properties = new Dictionary<String, Object>();
		}

		public void SetProperty(String name, Object value) {
			if (value == null) {
				_properties.Remove(name);
			} else {
				_properties.Add(name, value);
			}
		}

		public T GetProperty<T>(String name) {
			return (T) _properties[name];
		}

		void AbstractEventConductor(
			string eventName,
			Action<Type, String> f1,
			Action<Object, String> f2,
			Type catchException
		) {
			// try on static property
			try {
				f1(GetType(), eventName);
				return;
			} catch (Exception e) {
				if (!catchException.IsInstanceOfType(e)) {
					Debug.LogError(GetType());
					throw e;
				}
			}
			
			// try on instance property
			f2(this, eventName);
		}

		/// <summary>
		/// Useful utility for Offer some services (event) with the EventConductor.
		/// Detect automatically if this event is static or depend on instance
		/// </summary>
		/// <param name="eventName">Event name.</param>
		/// <exception cref="EventConductor.EventNotFoundException">throwed if the eventName isn't found.</exception>
		protected void Offer(string eventName) {
			AbstractEventConductor(
				eventName,
				EventConductor.Offer,
				EventConductor.Offer,
				typeof(EventConductor.EventNotFoundException)
			);
		}

		protected void Denial(string eventName) {
			AbstractEventConductor(
				eventName,
				EventConductor.Denial,
				EventConductor.Denial,
				typeof(EventConductor.EventNotRegisteredException)
			);
		}

		protected void On(string eventName) {
			AbstractEventConductor(
				eventName,
				EventConductor.On,
				EventConductor.On,
				typeof(EventConductor.CallbackNotFoundException)
			);
		}

		protected void Off(string eventName) {
			AbstractEventConductor(
				eventName,
				EventConductor.Off,
				EventConductor.Off,
				typeof(EventConductor.CallbackNotRegisteredException)
			);
		}
	}

}