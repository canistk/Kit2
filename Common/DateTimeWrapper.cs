using UnityEngine;
using DateTime = System.DateTime;

namespace Kit2
{
	[System.Serializable]
	public class DateTimeWrapper : ISerializationCallbackReceiver
	{
		[SerializeField] private long ticks;
		public DateTime dateTime;

		public static implicit operator DateTime(DateTimeWrapper dtw)
		{
			return dtw.dateTime;
		}

		public static implicit operator DateTimeWrapper(DateTime dt)
		{
			return new DateTimeWrapper() { dateTime = dt };
		}

		public void OnAfterDeserialize()
		{
			dateTime = new DateTime(ticks);
		}

		public void OnBeforeSerialize()
		{
			ticks = dateTime.Ticks;
		}
	}
}