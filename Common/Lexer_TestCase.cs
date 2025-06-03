using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kit2.Testcase;
namespace Kit2
{
	[CreateAssetMenu(fileName = "LexerTestCase", menuName = "Kit2/TestCase/Lexer")]
	public class Lexer_TestCase : TestCaseBase
	{
		[SerializeField] bool m_HideHardCodeTest = false;
		[System.Serializable]
		private struct ExtraCase
		{
			public bool expectedError;
			[TextArea(3, 10)]
			public string content;
		}

		[SerializeField] ExtraCase[] m_ExtraTextCase;


		public override IEnumerable<TestOperation> GetOperations()
		{
			if (!m_HideHardCodeTest)
			{
				yield return new LexerOperation(false, "one\ttwo\n\n\r\nfive\nseven\n\n\n");
				yield return new LexerOperation(false, "123 45.6 0x1234AF"); // numbers
				yield return new LexerOperation(false, "45.6\nn65.5"); // float float
				yield return new LexerOperation(true, "45.6.7"); // should fail float format.
				yield return new LexerOperation(false, "123 \"45.6.7\" string will ignore the float format."); // test block of string.
				yield return new LexerOperation(false, "0x123abcDEF"); // hex
				yield return new LexerOperation(true, "0x Should not work"); // hex format error.
				yield return new LexerOperation(false, "0x1 Should work");
				yield return new LexerOperation(true, "ab 0x123abcDEFgh"); // hex, should throw due to missing space before "gh"
				yield return new LexerOperation(false, "123 0x123abc\n0xFFF gh"); // hex newline should pass
				yield return new LexerOperation(false, "abc -123 0x123abc\n0xFFF gh"); // negative number
				yield return new LexerOperation(false, "abc 123-\n123"); // negative number 
			}
			if (m_ExtraTextCase != null)
			{
				foreach (var ele in m_ExtraTextCase)
				{
					yield return new LexerOperation(ele.expectedError, ele.content);
				}
			}
		}


		private class LexerOperation : TestOperation
		{
			private readonly string m_Input;
			private Lexer m_Lexer;
			public LexerOperation(bool expectedError, string input) : base(expectedError)
			{
				m_Input = input;
				m_Lexer = new Lexer(in input);
			}

			protected override bool InProgress()
			{
				while (m_Lexer.NextToken(out var token, eSkipMethods.SkipAll))
				{
				}
				return !m_Lexer.IsCompleted();
			}

			public override void OnInspecterDraw(out string debugText)
			{
				// base.OnInspecterDraw(out debugText);
				debugText = m_Input;
			}
		}
	}
}