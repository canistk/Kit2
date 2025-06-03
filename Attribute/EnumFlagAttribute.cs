namespace Kit2
{
	public class EnumFlagAttribute : UnityEngine.PropertyAttribute
	{
		public readonly System.Type type;
		/// <summary>
		/// Filter out the base values of Enum, which mean multiple value will be removed in the list. 
		/// </summary>
		public readonly bool IsSingleValue;
		public readonly bool IsNullable;
		public EnumFlagAttribute(System.Type enumType, bool isSingleValue, bool nullable = true)
		{
			this.type = enumType;
			this.IsSingleValue = isSingleValue;
			this.IsNullable = nullable;
		}
	}
}