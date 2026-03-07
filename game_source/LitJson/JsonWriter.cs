using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Ostranauts.Utils.Models;
using UnityEngine;

namespace LitJson
{
	public class JsonWriter
	{
		public JsonWriter()
		{
			this.inst_string_builder = new StringBuilder();
			this.writer = new StringWriter(this.inst_string_builder);
			this.Init();
		}

		public JsonWriter(StringBuilder sb) : this(new StringWriter(sb))
		{
		}

		public JsonWriter(TextWriter writer)
		{
			if (writer == null)
			{
				throw new ArgumentNullException("writer");
			}
			this.writer = writer;
			this.Init();
		}

		public int IndentValue
		{
			get
			{
				return this.indent_value;
			}
			set
			{
				this.indentation = this.indentation / this.indent_value * value;
				this.indent_value = value;
			}
		}

		public bool PrettyPrint
		{
			get
			{
				return this.pretty_print;
			}
			set
			{
				this.pretty_print = value;
			}
		}

		public TextWriter TextWriter
		{
			get
			{
				return this.writer;
			}
		}

		public bool Validate
		{
			get
			{
				return this.validate;
			}
			set
			{
				this.validate = value;
			}
		}

		private void DoValidation(Condition cond)
		{
			if (!this.context.ExpectingValue)
			{
				this.context.Count++;
			}
			if (!this.validate)
			{
				return;
			}
			if (this.has_reached_end)
			{
				throw new JsonException("A complete JSON symbol has already been written");
			}
			switch (cond)
			{
			case Condition.InArray:
				if (!this.context.InArray)
				{
					throw new JsonException("Can't close an array here");
				}
				break;
			case Condition.InObject:
				if (!this.context.InObject || this.context.ExpectingValue)
				{
					throw new JsonException("Can't close an object here");
				}
				break;
			case Condition.NotAProperty:
				if (this.context.InObject && !this.context.ExpectingValue)
				{
					throw new JsonException("Expected a property");
				}
				break;
			case Condition.Property:
				if (!this.context.InObject || this.context.ExpectingValue)
				{
					throw new JsonException("Can't add a property here");
				}
				break;
			case Condition.Value:
				if (!this.context.InArray && (!this.context.InObject || !this.context.ExpectingValue))
				{
					throw new JsonException("Can't add a value here");
				}
				break;
			}
		}

		private void Init()
		{
			this.has_reached_end = false;
			this.hex_seq = new char[4];
			this.indentation = 0;
			this.indent_value = 4;
			this.pretty_print = false;
			this.validate = true;
			this.ctx_stack = new Stack<WriterContext>();
			this.context = new WriterContext();
			this.ctx_stack.Push(this.context);
		}

		private static void IntToHex(int n, char[] hex)
		{
			for (int i = 0; i < 4; i++)
			{
				int num = n % 16;
				if (num < 10)
				{
					hex[3 - i] = (char)(48 + num);
				}
				else
				{
					hex[3 - i] = (char)(65 + (num - 10));
				}
				n >>= 4;
			}
		}

		private void Indent()
		{
			if (this.pretty_print)
			{
				this.indentation += this.indent_value;
			}
		}

		private void Put(string str)
		{
			if (this.pretty_print && !this.context.ExpectingValue)
			{
				for (int i = 0; i < this.indentation; i++)
				{
					this.writer.Write(' ');
				}
			}
			this.writer.Write(str);
		}

		private void PutNewline()
		{
			this.PutNewline(true);
		}

		private void PutNewline(bool add_comma)
		{
			if (add_comma && !this.context.ExpectingValue && this.context.Count > 1)
			{
				this.writer.Write(',');
			}
			if (this.pretty_print && !this.context.ExpectingValue)
			{
				this.writer.Write('\n');
			}
		}

		private void PutString(string str)
		{
			this.Put(string.Empty);
			this.writer.Write('"');
			int length = str.Length;
			for (int i = 0; i < length; i++)
			{
				char c = str[i];
				switch (c)
				{
				case '\b':
					this.writer.Write("\\b");
					break;
				case '\t':
					this.writer.Write("\\t");
					break;
				case '\n':
					this.writer.Write("\\n");
					break;
				default:
					if (c != '"' && c != '\\')
					{
						if (str[i] >= ' ' && str[i] <= '~')
						{
							this.writer.Write(str[i]);
						}
						else
						{
							JsonWriter.IntToHex((int)str[i], this.hex_seq);
							this.writer.Write("\\u");
							this.writer.Write(this.hex_seq);
						}
					}
					else
					{
						this.writer.Write('\\');
						this.writer.Write(str[i]);
					}
					break;
				case '\f':
					this.writer.Write("\\f");
					break;
				case '\r':
					this.writer.Write("\\r");
					break;
				}
			}
			this.writer.Write('"');
		}

		private void Unindent()
		{
			if (this.pretty_print)
			{
				this.indentation -= this.indent_value;
			}
		}

		public override string ToString()
		{
			if (this.inst_string_builder == null)
			{
				return string.Empty;
			}
			return this.inst_string_builder.ToString();
		}

		public void Reset()
		{
			this.has_reached_end = false;
			this.ctx_stack.Clear();
			this.context = new WriterContext();
			this.ctx_stack.Push(this.context);
			if (this.inst_string_builder != null)
			{
				this.inst_string_builder.Remove(0, this.inst_string_builder.Length);
			}
		}

		public void Write(bool boolean)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			this.Put((!boolean) ? "false" : "true");
			this.context.ExpectingValue = false;
		}

		public void Write(decimal number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			this.Put(Convert.ToString(number, JsonWriter.number_format));
			this.context.ExpectingValue = false;
		}

		public void Write(float number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			string text = Convert.ToString(number, JsonWriter.number_format);
			if (float.IsPositiveInfinity(number))
			{
				text = "9e99";
			}
			else if (float.IsNegativeInfinity(number))
			{
				text = "-9e99";
			}
			else if (float.IsNaN(number))
			{
				text = "0.001";
			}
			this.Put(text);
			if (text.IndexOf('.') == -1 && text.IndexOf('e') == -1 && text.IndexOf('E') == -1)
			{
				this.writer.Write(".0");
			}
			this.context.ExpectingValue = false;
		}

		public void Write(double number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			string text = Convert.ToString(number, JsonWriter.number_format);
			if (double.IsPositiveInfinity(number))
			{
				text = "9E99";
			}
			else if (double.IsNegativeInfinity(number))
			{
				text = "-9E99";
			}
			else if (double.IsNaN(number))
			{
				text = "0.001";
			}
			this.Put(text);
			if (text.IndexOf('.') == -1 && text.IndexOf('E') == -1)
			{
				this.writer.Write(".0");
			}
			this.context.ExpectingValue = false;
		}

		public void Write(int number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			this.Put(Convert.ToString(number, JsonWriter.number_format));
			this.context.ExpectingValue = false;
		}

		public void Write(long number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			this.Put(Convert.ToString(number, JsonWriter.number_format));
			this.context.ExpectingValue = false;
		}

		public void Write(string str)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			if (str == null)
			{
				this.Put("null");
			}
			else
			{
				this.PutString(str);
			}
			this.context.ExpectingValue = false;
		}

		public void Write(ulong number)
		{
			this.DoValidation(Condition.Value);
			this.PutNewline();
			this.Put(Convert.ToString(number, JsonWriter.number_format));
			this.context.ExpectingValue = false;
		}

		public void WriteArrayEnd()
		{
			this.DoValidation(Condition.InArray);
			this.PutNewline(false);
			this.ctx_stack.Pop();
			if (this.ctx_stack.Count == 1)
			{
				this.has_reached_end = true;
			}
			else
			{
				this.context = this.ctx_stack.Peek();
				this.context.ExpectingValue = false;
			}
			this.Unindent();
			this.Put("]");
		}

		public void WriteArrayStart()
		{
			this.DoValidation(Condition.NotAProperty);
			this.PutNewline();
			this.Put("[");
			this.context = new WriterContext();
			this.context.InArray = true;
			this.ctx_stack.Push(this.context);
			this.Indent();
		}

		public void WriteObjectEnd()
		{
			this.DoValidation(Condition.InObject);
			this.PutNewline(false);
			this.ctx_stack.Pop();
			if (this.ctx_stack.Count == 1)
			{
				this.has_reached_end = true;
			}
			else
			{
				this.context = this.ctx_stack.Peek();
				this.context.ExpectingValue = false;
			}
			this.Unindent();
			this.Put("}");
		}

		public void WriteObjectStart()
		{
			this.DoValidation(Condition.NotAProperty);
			this.PutNewline();
			this.Put("{");
			this.context = new WriterContext();
			this.context.InObject = true;
			this.ctx_stack.Push(this.context);
			this.Indent();
		}

		public void WritePropertyName(string property_name)
		{
			this.DoValidation(Condition.Property);
			this.PutNewline();
			this.PutString(property_name);
			if (this.pretty_print)
			{
				if (property_name.Length > this.context.Padding)
				{
					this.context.Padding = property_name.Length;
				}
				for (int i = this.context.Padding - property_name.Length; i >= 0; i--)
				{
					this.writer.Write(' ');
				}
				this.writer.Write(": ");
			}
			else
			{
				this.writer.Write(':');
			}
			this.context.ExpectingValue = true;
		}

		public void Write(Vector2 v2)
		{
			this.WriteObjectStart();
			this.WritePropertyName("x");
			this.Write(v2.x);
			this.WritePropertyName("y");
			this.Write(v2.y);
			this.WriteObjectEnd();
		}

		public void Write(Vector3 v3)
		{
			this.WriteObjectStart();
			this.WritePropertyName("x");
			this.Write(v3.x);
			this.WritePropertyName("y");
			this.Write(v3.y);
			this.WritePropertyName("z");
			this.Write(v3.z);
			this.WriteObjectEnd();
		}

		public void Write(Vector4 v4)
		{
			this.WriteObjectStart();
			this.WritePropertyName("x");
			this.Write(v4.x);
			this.WritePropertyName("y");
			this.Write(v4.y);
			this.WritePropertyName("z");
			this.Write(v4.z);
			this.WritePropertyName("z");
			this.Write(v4.w);
			this.WriteObjectEnd();
		}

		public void Write(Quaternion q)
		{
			this.WriteObjectStart();
			this.WritePropertyName("x");
			this.Write(q.x);
			this.WritePropertyName("y");
			this.Write(q.y);
			this.WritePropertyName("z");
			this.Write(q.z);
			this.WritePropertyName("z");
			this.Write(q.w);
			this.WriteObjectEnd();
		}

		public void Write(Matrix4x4 m)
		{
			this.WriteObjectStart();
			this.WritePropertyName("m00");
			this.Write(m.m00);
			this.WritePropertyName("m33");
			this.Write(m.m33);
			this.WritePropertyName("m23");
			this.Write(m.m23);
			this.WritePropertyName("m13");
			this.Write(m.m13);
			this.WritePropertyName("m03");
			this.Write(m.m03);
			this.WritePropertyName("m32");
			this.Write(m.m32);
			this.WritePropertyName("m12");
			this.Write(m.m12);
			this.WritePropertyName("m02");
			this.Write(m.m02);
			this.WritePropertyName("m22");
			this.Write(m.m22);
			this.WritePropertyName("m21");
			this.Write(m.m21);
			this.WritePropertyName("m11");
			this.Write(m.m11);
			this.WritePropertyName("m01");
			this.Write(m.m01);
			this.WritePropertyName("m30");
			this.Write(m.m30);
			this.WritePropertyName("m20");
			this.Write(m.m20);
			this.WritePropertyName("m10");
			this.Write(m.m10);
			this.WritePropertyName("m31");
			this.Write(m.m31);
			this.WriteObjectEnd();
		}

		public void Write(Ray r)
		{
			this.WriteObjectStart();
			this.WritePropertyName("origin");
			this.Write(r.origin);
			this.WritePropertyName("direction");
			this.Write(r.direction);
			this.WriteObjectEnd();
		}

		public void Write(RaycastHit r)
		{
			this.WriteObjectStart();
			this.WritePropertyName("barycentricCoordinate");
			this.Write(r.barycentricCoordinate);
			this.WritePropertyName("distance");
			this.Write(r.distance);
			this.WritePropertyName("normal");
			this.Write(r.normal);
			this.WritePropertyName("point");
			this.Write(r.point);
			this.WriteObjectEnd();
		}

		public void Write(Color c)
		{
			this.WriteObjectStart();
			this.Put(string.Format("\"r\":{0},\"g\":{1},\"b\":{2},\"a\":{3}", new object[]
			{
				c.r,
				c.g,
				c.b,
				c.a
			}));
			this.WriteObjectEnd();
		}

		public void WritePoint(Point pt)
		{
			this.WriteObjectStart();
			this.WritePropertyName("X");
			this.Write(pt.X);
			this.WritePropertyName("Y");
			this.Write(pt.Y);
			this.WriteObjectEnd();
		}

		private static NumberFormatInfo number_format = NumberFormatInfo.InvariantInfo;

		private WriterContext context;

		private Stack<WriterContext> ctx_stack;

		private bool has_reached_end;

		private char[] hex_seq;

		private int indentation;

		private int indent_value;

		private StringBuilder inst_string_builder;

		private bool pretty_print;

		private bool validate;

		private TextWriter writer;
	}
}
