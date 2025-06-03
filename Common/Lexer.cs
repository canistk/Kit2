using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Kit2
{
	public enum eToken
	{
		Unexpected,
		Space,
		Integer,
		Float,
		Hexadecimal,
		Identifier,

		Operator,
		//LeftParen,
		//RightParen,
		//LeftSquare,
		//RightSquare,
		//LeftCurly,
		//RightCurly,
		//LessThan,
		//GreaterThan,
		//Equal,
		//Plus,
		//Minus,
		//Asterisk,
		//Slash,
		//Hash,
		//Dot,
		//Comma,
		//Colon,
		//Semicolon,
		//SingleQuote,
		//DoubleQuote,
		BlockOfString,
		BlockOfComment,
		Comment,
		//Pipe,
		EndOfLine,
	}

	public readonly struct Token : IEquatable<Token>
	{
		public readonly eToken kind;
		public readonly string value;
		public Token(eToken kind, char value) : this(kind, value.ToString()) { }
		public Token(eToken kind, ReadOnlyMemory<char> content, int start, int endPlusOne)
		{
			var len = endPlusOne - start;
			Debug.Assert(len > 0, $"logic error, negative is smaller then zero. start={start},end1={endPlusOne} == {len}");
			this.kind = kind;
			this.value = content.Slice(start, len).ToString();
		}
		public Token(eToken kind, string value)
		{
			this.kind = kind;
			this.value = value;
		}

		public bool Equals(Token other)
		{
			return kind == other.kind && value.Equals(other.value);
		}

		public override string ToString()
		{
			return $"T:{kind}=\"{value}\"";
		}

		public bool IsNone() => kind == eToken.Unexpected;
		public bool IsSpace() => kind == eToken.Space;
		public bool IsInteger() => kind == eToken.Integer;
		public bool IsNumber() => kind == eToken.Integer || kind == eToken.Float || kind == eToken.Hexadecimal;
		public bool IsFloat() => kind == eToken.Float;
		public bool IsHexadecimal() => kind == eToken.Hexadecimal;
		public bool IsBlockOfString() => kind == eToken.BlockOfString;
		public bool IsComment() => kind == eToken.Comment;
		public bool IsBlockOfComment() => kind == eToken.BlockOfComment;
		public bool IsNewLine() => kind == eToken.EndOfLine;

		public bool IsIdentifier() => kind == eToken.Identifier;
		public bool IsIdentifier(string s, bool ignoreCase = false) => kind == eToken.Identifier && value.Equals(s, (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
		public bool IsIdentifier(char c) => kind == eToken.Identifier && value.Length == 1 && value[0] == c;

		public bool IsOperator() => kind == eToken.Operator;
		public bool IsOperator(string s, bool ignoreCase = false) => kind == eToken.Operator && value.Equals(s, (ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
		public bool IsOperator(char c) => kind == eToken.Operator && value.Length == 1 && value[0].Equals(c);
		/// <summary>Compare if any of giving string are matching this token.</summary>
		/// <param name="arr"></param>
		/// <returns>true = found matching.</returns>
		public bool IsIdentifier(bool ignoreCase = false, params string[] arr)
		{
			if (kind != eToken.Identifier)
				return false;
			var strLen = value.Length;
			var ignore = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			for (int i = 0; i < arr.Length; ++i)
			{
				if (arr[i].Length == strLen && arr[i].Equals(value, ignore))
					return true;
			}
			return false;
		}

		/// <summary>Compare if any of giving string are matching this token.</summary>
		/// <param name="arr"></param>
		/// <returns>true = found matching.</returns>
		public bool IsOperator(bool ignoreCase = false, params char[] arr)
		{
			if (kind != eToken.Operator)
				return false;
			if (value.Length != 1)
				return false; // operator only 1 character

			var ch = value[0];
			for (int i = 0; i < arr.Length; ++i)
			{
				if (ignoreCase)
				{
					if (ch.ToString().Equals(arr[i].ToString(), StringComparison.OrdinalIgnoreCase))
						return true;
				}
				else
				{
					if (ch.Equals(arr[i]))
						return true;
				}
			}
			return false;
		}

		/// <summary>Compare if any of giving string are matching this token.</summary>
		/// <param name="arr"></param>
		/// <returns>true = found matching.</returns>
		public bool IsOperator(bool ignoreCase = false, params string[] arr)
		{
			if (kind != eToken.Operator)
				return false;
			var ignore = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
			for (int i = 0; i < arr.Length; ++i)
			{
				if (value.Equals(arr[i], ignore))
					return true;
			}
			return false;
		}
	}

	[System.Flags]
	public enum eSkipMethods
	{
		None = 0,
		SkipSpace = 1 << 0,
		SkipNewline = 1 << 1,
		SkipAll = SkipSpace | SkipNewline,
	}

	public class LexerException : Exception
	{
		public LexerException(Lexer lexer) : base(lexer.GetPointerInfo()) { }
		public LexerException(Lexer lexer, string message) : base(WrapMsg(lexer, message)) { }

		private static string WrapMsg(Lexer lexer, string msg)
		{
			return $"{msg}\r\n{lexer.GetPointerInfo()}";
		}
	}

	public class Lexer
	{
		public Lexer(in string content)
		{
			m_Content = content.AsMemory();
			m_Line = m_Column = m_Anchor = 0;
		}

		#region Pointer
		/// <summary>Processing Line</summary>
		private int m_Line;
		public int GetLine() => m_Line;

		/// <summary>Processing Column</summary>
		private int m_Column;
		public int GetColumn() => m_Column;

		/// <summary>Processing character</summary>
		private int m_Anchor;
		public int GetAnchor() => m_Anchor;

		/// <summary>Content to process</summary>
		private ReadOnlyMemory<char> m_Content;
		public string GetContent() => m_Content.ToString();

		public int GetContentLen() => m_Content.Length;

		public string GetContentSubstring(int sidx, int len)
		{
			return m_Content.Slice(sidx, len).ToString();
		}

		/// <summary>Anchor within process content length.</summary>
		private bool IsOutOfChar() => m_Anchor >= m_Content.Length;

		public bool IsBegin => m_Anchor > 0;
		/// <summary>move to next character</summary>
		/// <returns>
		/// true = pointer moved to next char
		/// false = end of file.
		/// </returns>
		private bool NextChar()
		{
			++m_Column;
			++m_Anchor;
			if (IsOutOfChar())
				return false;
			if (m_Content.Span[m_Anchor] == '\n')
			{
				++m_Line;
				m_Column = 0;
			}
			return true;
		}
		/// <summary>Current pointing 'character' within content.</summary>
		/// <returns></returns>
		/// <exception cref="System.IndexOutOfRangeException"></exception>
		private ReadOnlySpan<char> Current()
		{
			if (IsOutOfChar())
				throw new System.IndexOutOfRangeException();
			return m_Content.Span.Slice(m_Anchor, 1);
		}

		/// <summary>return next character without moving the anchor</summary>
		/// <param name="peek">peek next character.</param>
		/// <returns></returns>
		private bool TryPeek(out ReadOnlySpan<char> peek)
		{
			peek = default;
			if (m_Anchor + 1 >= m_Content.Length)
				return false;
			peek = m_Content.Span.Slice(m_Anchor + 1, 1);
			return true;
		}

		/// <summary>return previous character without moving the anchor</summary>
		/// <param name="last">take previous character.</param>
		/// <returns></returns>
		private bool TryPrev(out ReadOnlySpan<char> last)
		{
			last = default;
			if (m_Anchor == 0)
				return false;
			last = m_Content.Span.Slice(m_Anchor - 1, 1);
			return true;
		}

		public string GetPointerInfo()
		{
			GetLastFewLines(out var start, out var length, 3);
			var substring = m_Content.Slice(start, length);
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append(substring).AppendLine();
			for (int i = 1; i < m_Column; ++i)
			{
				sb.Append('-');
			}
			sb.Append('^').AppendLine();
			sb.Append("Pointer: Line ").Append(m_Line)
				.Append(" Column:").Append(m_Column)
				.AppendLine();
			return sb.ToString();
		}
		public void GetLastFewLines(out int start, out int length, int line = 3)
		{
			if (m_Content.Length == 0)
			{
				start = 0; length = 0;
				return;
			}

			var anchor = m_Anchor;
			if (m_Content.Length < anchor)
			{
				start = 0;
				length = m_Content.Length;
				return;
			}

			var end = anchor;
			while (m_Content.Span[end] != '\n' &&
				++end < m_Content.Length)
			{ }

			start = anchor = m_Anchor;
			while (start > 0 && --anchor > 0)
			{
				if (m_Content.Span[anchor] == '\n')
					--line;
				if (line <= 0)
				{
					start = anchor;
					length = end - start;
					return;
				}
			}
			// if running out of line
			start = 0;
			length = end;
		}
		#endregion Pointer

		private Token m_Token;
		public Token token => m_Token;
		private void UpdateToken(Token token)
		{
			m_Token = token;
		}

		/// <summary>
		/// Move to next token, and define the token type based on reading character.
		/// also move the anchor to the end of current token type.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="methods"></param>
		/// <returns>
		/// true = successful define token
		/// false = fail to recognize token.
		/// </returns>
		private bool _NextToken(out Token token, eSkipMethods methods)
		{
			token = new Token(eToken.Unexpected, null);
			if (IsOutOfChar())
				return false;

			var c = Current();

			// handle spaces
			if (IsSpace(c))
			{
				if ((methods & eSkipMethods.SkipSpace) != 0)
				{
					ProcessSpace(out token);
					if (IsOutOfChar())
						return false;
					// Skip all space and continue.
					c = Current();
				}
				else
				{
					ProcessSpace(out token);
					return !IsOutOfChar();
				}
			}

			// handle identify
			// bias with "_" underline prefix
			if (c[0] == '_' || IsAlpha(c))
			{
				ProcessIdentify(out token);
				return true;
			}

			// handle numbers
			if (IsDigit(c))
			{
				ProcessNumber(false, out token);
				return true;
			}
			// special handle for "-" minus operator or negative number
			else if (c[0] == '-' &&
				TryPeek(out var nexChar) &&
				(IsDigit(nexChar) || IsHexadecimal(nexChar)))
			{
				ProcessNumber(true, out token);
				return true;
			}

			// handle block of string
			if (c[0] == '\"')
			{
				ProcessBlockOfString(out token);
				return true;
			}

			// newline
			if (c[0] == '\n')
			{
				if ((methods & eSkipMethods.SkipNewline) != 0)
				{
					ProcessNewline(true, out _);
					return _NextToken(out token, methods);
				}
				ProcessNewline(false, out token);
				return true;
			}

			// escape characters
			if (c[0] == '/')
			{
				ProcessComments(out token);
				return true;
			}

			// when above cases fail, this just a operator, let parser define the meaning of this.
			ProcessOperator(out token);
			return true;
		}

		private bool _NextTokenAsString(out Token token, eSkipMethods methods)
		{
			token = new Token(eToken.Unexpected, null);
			if (IsOutOfChar())
				return false;

			var c = Current();

			// handle spaces
			if (IsSpace(c))
			{
				if ((methods & eSkipMethods.SkipSpace) != 0)
				{
					ProcessSpace(out token);
					if (IsOutOfChar())
						return false;
					// Skip all space and continue.
					c = Current();
				}
				else
				{
					ProcessSpace(out token);
					return !IsOutOfChar();
				}
			}

			// handle identify & digit are same.
			// bias with "_" underline prefix
			if (c[0] == '_' || IsAlpha(c) || IsDigit(c))
			{
				ProcessIdentify(out token);
				return true;
			}

			// newline
			if (c[0] == '\n')
			{
				if ((methods & eSkipMethods.SkipNewline) != 0)
				{
					ProcessNewline(true, out _);
					return _NextToken(out token, methods);
				}
				ProcessNewline(false, out token);
				return true;
			}

			// when above cases fail, this just a operator, let parser define the meaning of this.
			ProcessOperator(out token);
			return true;
		}

		/// <summary>
		/// Without process following :
		/// numbers         (digit, float) 
		/// String block    (")
		/// comment         (/*)
		/// comment line    (//)
		/// </summary>
		/// <param name="methods"></param>
		/// <returns></returns>
		public bool NextTokenAsString(eSkipMethods methods = eSkipMethods.SkipAll)
		{
			var rst = _NextTokenAsString(out var token, methods);
			UpdateToken(token);
			return rst;
		}

		/// <summary>
		/// Without process following :
		/// numbers         (digit, float) 
		/// String block    (")
		/// comment         (/*)
		/// comment line    (//)
		/// </summary>
		/// <param name="methods"></param>
		/// <returns></returns>
		public bool NextTokenAsString(out Token token, eSkipMethods methods = eSkipMethods.SkipAll)
		{
			var rst = _NextTokenAsString(out token, methods);
			UpdateToken(token);
			return rst;
		}

		/// <summary>
		/// Move to next token, and define the token type based on reading character.
		/// also move the anchor to the end of current token type.
		/// access token <see cref="Lexer.token"/>
		/// </summary>
		/// <param name="methods"></param>
		/// <returns>
		/// true = successful define token
		/// false = fail to recognize token.
		/// </returns>
		public bool NextToken(eSkipMethods methods = eSkipMethods.SkipAll)
		{
			var rst = _NextToken(out var token, methods);
			UpdateToken(token);
			return rst;
		}

		/// <summary>
		/// Move to next token, and define the token type based on reading character.
		/// also move the anchor to the end of current token type.
		/// </summary>
		/// <param name="token"></param>
		/// <param name="methods"></param>
		/// <returns>
		/// true = successful define token
		/// false = fail to recognize token.
		/// </returns>
		public bool NextToken(out Token token, eSkipMethods methods = eSkipMethods.SkipAll)
		{
			var rst = _NextToken(out token, methods);
			UpdateToken(token);
			return rst;
		}

		public bool IsCompleted()
		{
			return IsOutOfChar();
		}

		public bool SkipNewlineToken()
		{
			if (IsOutOfChar())
				return false;
			while (IsNewLine(Current()) && NextChar()) { } /* skip */
			return !IsOutOfChar();
		}



		#region Char checker
		private bool IsSpace(ReadOnlySpan<char> c) { return c[0] == ' ' || c[0] == '\t' || c[0] == '\r'; }
		private bool IsNewLine(ReadOnlySpan<char> c) { return c[0] == '\n'; }
		private bool IsLowerCase(ReadOnlySpan<char> c) { return c[0] >= 'a' && c[0] <= 'z'; }
		private bool IsUpperCase(ReadOnlySpan<char> c) { return c[0] >= 'A' && c[0] <= 'Z'; }
		private bool IsAlpha(ReadOnlySpan<char> c) { return IsLowerCase(c) || IsUpperCase(c); }
		private bool IsDigit(ReadOnlySpan<char> c) { return c[0] >= '0' && c[0] <= '9'; }
		private bool IsHexadecimal(ReadOnlySpan<char> c) { return IsDigit(c) || (c[0] >= 'a' && c[0] <= 'f') || (c[0] >= 'A' && c[0] <= 'F'); }
		private bool IsEndOfLine(ReadOnlySpan<char> c) { return c[0] == '\n'; }
		#endregion Char checker

		#region Token Kind Handler
		/// <summary>Combine all space(s) into single token.</summary>
		private void ProcessSpace(out Token token)
		{
			if (!IsSpace(Current()))
				throw new System.Exception($"Programming Error : {nameof(ProcessSpace)}");
			token = new Token(eToken.Space, ' ');
			while (IsSpace(Current()) && NextChar()) { }
		}

		private void ProcessNewline(bool collapse, out Token token)
		{
			if (Current()[0] != '\n')
				throw new System.Exception($"Programming Error : {nameof(ProcessNewline)}");

			while (NextChar() && IsNewLine(Current()) && collapse)
			{ }

			token = new Token(eToken.EndOfLine, '\n');
		}

		/// <summary>
		/// token for any identify,
		/// anything start from "_" or "A~Z" or "a~z"
		/// but allow the second character can be "0~9" afterward.
		/// usually used on command, variable, special naming
		/// </summary>
		/// <returns></returns>
		private void ProcessIdentify(out Token token)
		{
			var start = m_Anchor;
			ReadOnlySpan<char> c;
			do
			{
				c = Current();

				if (!(c[0] == '_' || IsAlpha(c) || IsDigit(c)))
					break;
			}
			while (NextChar() && _Rules(Current()));

			var diff = m_Anchor - start;
			Debug.Assert(diff >= 0, $"logic error, {diff} is smaller then zero.");
			token = new Token(eToken.Identifier, m_Content, start, m_Anchor);

			bool _Rules(ReadOnlySpan<char> c)
			{
				switch (c[0])
				{
					case '\n': return false;
					case '_': return true;
				}
				return IsAlpha(c) || IsDigit(c);
			}
		}


		/// <summary>
		/// Number, Hexadecimal, Float
		/// </summary>
		/// <param name="isNegative"></param>
		/// <param name="token"></param>
		/// <returns></returns>
		/// <exception cref="LexerException"></exception>
		private void ProcessNumber(bool isNegative, out Token token)
		{
			var start = m_Anchor;


			var c = Current();
			var isHex = c[0] == '0' && TryPeek(out var nextChar) && nextChar[0] == 'x';
			if (isHex)
			{
				NextChar(); // skip x
				if (!NextChar())
					throw new LexerException(this, $"Unexpect end of hexadecimal.");
			}
			if (isNegative && isHex)
				throw new LexerException(this, "Didn't support negative hexadecimal.");
			if (isNegative && c[0] == '-')
			{
				NextChar(); // consume '-'
			}

			var hasDot = false;
			do
			{
				c = Current();
				if (isHex)
				{
					if (IsHexadecimal(c))
						continue;
					else if (m_Anchor - start <= 2)
						throw new LexerException(this, "Hexadecimal format error.");
					else if (IsAlpha(c))
						throw new LexerException(this, $"input character \"{c[0]}\" are not Hexadecimal.");

					// until it's not Hexadecimal
					break;
				}
				else if (c[0] == '.')
				{
					if (!hasDot)
					{
						hasDot = true;
					}
					else
					{
						// had double dot 
						throw new LexerException(this, $"invalid float format detected, double dot within a number.");
					}
					if (IsHexadecimal(c))
						throw new LexerException(this, $"Hexadecimal format not support floating number '.'");
				}
				else if (!IsDigit(c))
				{
					break;
				}
			}
			while (!IsOutOfChar() && !IsEndOfLine(c) && NextChar());

			var kind =
				isHex ? eToken.Hexadecimal :
				hasDot ? eToken.Float :
				eToken.Integer;
			token = new Token(kind, m_Content, start, m_Anchor);
		}

		private void ProcessBlockOfString(out Token token)
		{
			if (Current()[0] != '\"' || !NextChar())
				throw new System.Exception($"Programmnig Error : {nameof(ProcessBlockOfString)}");

			var sb = new System.Text.StringBuilder(128);
			ReadOnlySpan<char> c;
			do
			{
				c = Current();

				if (c[0] == '\"')
				{
					break;// exit loop
				}
				else if (c[0] == '\\') // Escape character
				{
					if (!NextChar())
						throw new LexerException(this, $"Unexpected end of {nameof(ProcessBlockOfString)}");
					c = Current();
					char ch = c[0] switch
					{
						'\\' => '\\',
						'\'' => '\'',
						'"' => '\"',
						'a' => '\a',
						'b' => '\b',
						'f' => '\f',
						'n' => '\n',
						'r' => '\r',
						't' => '\t',
						'v' => '\v',
						_ => throw new LexerException(this, $"\"\\{c[0]}\", Unexpected character. "),
					};
					sb.Append(c[0]).Append(ch);
				}
				else
				{
					sb.Append(c[0]);
				}
			}
			while (!IsOutOfChar() && NextChar());

			// End of string
			token = new Token(eToken.BlockOfString, sb.ToString());
			if (c[0] == '\"')
				NextChar(); // consume
		}

		private void ProcessComments(out Token token)
		{
			if (Current()[0] != '/')
				throw new Exception($"Programming Error : {nameof(ProcessComments)}");

			// the last character of the content is "slash"
			token = new Token(eToken.Operator, '/');
			if (!NextChar())
				return; // early return;

			// Extra 
			var c = Current();
			if (c[0] == '*')
			{
				// Comment block
				ProcessCommentBlock(out token);
				return;
			}
			else if (c[0] == '/')
			{
				// Single Comment line.
				ProcessSingleCommentLine(out token);
				return;
			}

			// when above cases fail, this token just a "slash", but we let parser to handle the meaning of this.
		}

		private void ProcessCommentBlock(out Token token)
		{
			var c = Current();
			if (c[0] != '*')
				throw new System.Exception($"Programming Error {nameof(ProcessCommentBlock)}");
			var start = m_Anchor - 1;
			Debug.Assert(start >= 0, $"Programming Error {nameof(ProcessCommentBlock)} start index < 0");

			do
			{
				if (Current()[0] == '*' && TryPeek(out var nextChar) &&
					nextChar[0] == '/')
				{
					NextChar();
					token = new Token(eToken.BlockOfComment, m_Content, start, m_Anchor);
					return;
				}
			}
			while (!IsOutOfChar() && NextChar());

			throw new LexerException(this, $"{nameof(ProcessCommentBlock)}, fail to locate the end of comment block \"*/\" pattern.");
		}

		private void ProcessSingleCommentLine(out Token token)
		{
			var c = Current();
			if (c[0] != '/')
				throw new System.Exception($"Programming Error {nameof(ProcessSingleCommentLine)}");
			NextChar();
			var start = m_Anchor;
			do
			{
				if (IsNewLine(Current()))
					break;
			}
			while (!IsOutOfChar() && NextChar() && !IsNewLine(Current()));

			token = new Token(eToken.Comment, m_Content, start, m_Anchor);
		}

		private void ProcessOperator(out Token token)
		{
			var start = m_Anchor;
			var c = Current();
			if ((c[0] == '>' || c[0] == '<' || c[0] == '!' || c[0] == '=') &&
				TryPeek(out var nextChar) && nextChar[0] == '=')
			{
				// Special handle case >=, <=, !=, ==
				// operator more than 1 character.
				NextChar(); // consume operator
				NextChar(); // consume '='
				token = new Token(eToken.Operator, m_Content, start, m_Anchor);
				return;
			}


			NextChar(); // consume operator
			token = new Token(eToken.Operator, c[0]);
		}

		public override string ToString()
		{
			return $"lexer content:\n{m_Content}\n\nPointer Info :\n{GetPointerInfo()}";
		}
		#endregion Token Kind Handler
	}
}
