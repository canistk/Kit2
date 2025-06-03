namespace Kit2
{
	public class MaskFieldAttribute : UnityEngine.PropertyAttribute
	{
		public readonly System.Type type;
		public MaskFieldAttribute(System.Type enumType)
		{
			this.type = enumType;
		}
	}
}