using UnityEngine;
using UnityEditor;
using DateTime = System.DateTime;

namespace Kit2
{
	[CustomPropertyDrawer(typeof(DateTimeWrapper))]
	public class DateTimeWrapperDrawer : PropertyDrawer
	{
		static readonly float lineH = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		static readonly string[] m_MonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
		static readonly string[] m_WeekName = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
		static GUIStyle selectedButton = new GUIStyle(GUI.skin.button) { normal = new GUIStyleState() { textColor = Color.red, background = GUI.skin.button.normal.background } };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// prepare datetime.
			position = EditorGUI.IndentedRect(position);
			int orgIndent = EditorGUI.indentLevel;
			EditorGUI.indentLevel = 0;
			EditorGUI.BeginProperty(position, label, property);
			SerializedProperty ticksProp = property.FindPropertyRelative("ticks");
			long ticks = ticksProp.longValue;
			DateTime dateTime = new DateTime(ticks);
			Rect line = position.Clone(height: lineH);

			string title = string.Format("{0} : {1:u}", label.text, dateTime);
			property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, title, true);
			if (property.isExpanded)
			{
				Rect[] cols;
				EditorGUI.BeginChangeCheck();
				line = line.GetRectBottom();
				cols = line.SplitHorizontalUnclamp(true, 100f, 20f, 40f, 80f);
				int month = dateTime.Month;
				month = 1 + EditorGUI.Popup(cols[0], month - 1, m_MonthNames);

				int year = dateTime.Year;
				EditorGUI.LabelField(cols[2], "Year");
				year = EditorGUI.DelayedIntField(cols[3], year);

				line = line.GetRectBottom();
				line.y += 3f;
				DateTime firstDayInMonth = new DateTime(year, month, 1);
				int firstDayOfWeek = (int)firstDayInMonth.DayOfWeek; // first display on colume(s)
				int lastDayOfMonth = DateTime.DaysInMonth(year, month);
				Rect[] weekCols = line.Adjust(x: 3f).SplitHorizontalUnclamp(false, .14f, .14f, .14f, .14f, .14f, .14f, .14f);
				int drawDay = 0;
				int day = dateTime.Day;
				for (int cell = 0; cell < 7; cell++)
				{
					EditorGUI.LabelField(weekCols[cell], m_WeekName[cell], EditorStyles.miniButtonMid);
					weekCols[cell] = weekCols[cell].GetRectBottom();
				}
				line = line.GetRectBottom();

				for (int cell = 0; cell < firstDayOfWeek + lastDayOfMonth; cell++)
				{
					int i = cell % 7;
					if (cell >= firstDayOfWeek)
					{
						++drawDay;
						if (GUI.Button(weekCols[i], drawDay.ToString(), drawDay == day ? selectedButton : EditorStyles.miniButton))
						{
							day = drawDay;
						}
					}
					weekCols[i] = weekCols[i].GetRectBottom();
					if (i == 0)
						line = line.GetRectBottom();
				}

				line.y += 4f;
				cols = line.SplitHorizontalUnclamp(true, 50f, 30f, 25f, 30f, 25f, 30f, 25f);
				int x = 0;
				EditorGUI.LabelField(cols[x++], "Time:", EditorStyles.miniLabel);

				int hour = dateTime.Hour;
				hour = EditorGUI.IntField(cols[x++], GUIContent.none, hour);
				EditorGUI.LabelField(cols[x++], "Hr", EditorStyles.miniLabel);

				int minute = dateTime.Minute;
				minute = EditorGUI.DelayedIntField(cols[x++], GUIContent.none, minute);
				EditorGUI.LabelField(cols[x++], "Min", EditorStyles.miniLabel);

				int second = dateTime.Second;
				second = EditorGUI.DelayedIntField(cols[x++], GUIContent.none, second);
				EditorGUI.LabelField(cols[x++], "Sec", EditorStyles.miniLabel);

				if (EditorGUI.EndChangeCheck())
				{
					int maxDay = DateTime.DaysInMonth(year, month);
					year = year < 1 ? 1 : year;
					day = Mathf.Clamp(day, 1, maxDay);
					hour = Mathf.Clamp(hour, 0, 23);
					minute = Mathf.Clamp(minute, 0, 59);
					second = Mathf.Clamp(second, 0, 59);
					dateTime = new DateTime(year, month, day, hour, minute, second);
					ticksProp.longValue = dateTime.Ticks;
				}

				line = line.GetRectBottom();
				cols = line.SplitRight(80f);
				if (GUI.Button(cols[1], "Clear"))
				{
					dateTime = default(DateTime);
					ticksProp.longValue = dateTime.Ticks;
				}

				cols = cols[0].SplitRight(80f);
				if (GUI.Button(cols[1], "Today"))
				{
					dateTime = DateTime.Now.Date;
					ticksProp.longValue = dateTime.Ticks;
				}
			}
			EditorGUI.EndProperty();
			EditorGUI.indentLevel = orgIndent;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return property.isExpanded ? lineH * 10f + 10f : lineH;
		}
	}
}