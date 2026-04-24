using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using System.Reflection;
using System;
using System.Linq;
using OfficeOpenXml;
using System.Globalization;

public class ExcelUtility
{
	/// <summary>
	/// 第一行表头
	/// </summary>
	private List<string> mHeaders;

	/// <summary>
	/// 数据行（不含表头）
	/// </summary>
	private List<List<object>> mRows;

	/// <summary>
	/// 字段类型（与mHeaders一一对应）
	/// </summary>
	private List<string> mFieldTypes;

	/// <summary>
	/// 构造函数
	/// </summary>
	/// <param name="excelFile">Excel file.</param>
	public ExcelUtility(string excelFile)
	{
		mHeaders = null;
		mRows = null;
		mFieldTypes = null;

		try
		{
			if (string.IsNullOrEmpty(excelFile) || !File.Exists(excelFile))
			{
				Debug.LogError("ExcelUtility: Excel文件不存在，路径=" + excelFile);
				return;
			}

			string extension = Path.GetExtension(excelFile).ToLowerInvariant();
			if (extension != ".xlsx" && extension != ".xls")
			{
				Debug.LogError("ExcelUtility: 仅支持 .xlsx/.xls 文件，当前=" + extension);
				return;
			}

			using (FileStream stream = File.Open(excelFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			using (ExcelPackage package = new ExcelPackage(stream))
			{
				ExcelWorksheet sheet = package.Workbook.Worksheets.FirstOrDefault();
				if (sheet == null)
				{
					Debug.LogError("ExcelUtility: 工作簿中没有可读取的Sheet，路径=" + excelFile);
					return;
				}

				if (sheet.Dimension == null)
				{
					Debug.LogError("ExcelUtility: Sheet为空，路径=" + excelFile + "，Sheet=" + sheet.Name);
					return;
				}

				int rowCount = sheet.Dimension.End.Row;
				int colCount = sheet.Dimension.End.Column;
				if (rowCount < 1 || colCount < 1)
				{
					Debug.LogError("ExcelUtility: Sheet行列无效，路径=" + excelFile + "，Sheet=" + sheet.Name);
					return;
				}

				const int sideRowIndex = 3;   // 第3行：对应端（前端/后端）
				const int typeRowIndex = 4;   // 第4行：数据类型
				const int fieldRowIndex = 5;  // 第5行：参数名称
				const int dataStartRowIndex = 6; // 第6行开始：数据

				if (rowCount < fieldRowIndex)
				{
					Debug.LogError("ExcelUtility: 表头行不足，至少需要5行，路径=" + excelFile + "，Sheet=" + sheet.Name);
					return;
				}

				List<int> validColumnIndexes = new List<int>();
				mHeaders = new List<string>(colCount);
				mFieldTypes = new List<string>(colCount);
				for (int col = 1; col <= colCount; col++)
				{
					// 第3行用于标记前后端，目前不参与导出字段生成
					string sideText = sheet.Cells[sideRowIndex, col].Text;
					string typeText = sheet.Cells[typeRowIndex, col].Text;
					string fieldText = sheet.Cells[fieldRowIndex, col].Text;
					if (string.IsNullOrWhiteSpace(typeText) || string.IsNullOrWhiteSpace(fieldText))
					{
						continue;
					}

					validColumnIndexes.Add(col);
					mHeaders.Add(fieldText);
					mFieldTypes.Add(typeText.Trim().ToLowerInvariant());
				}

				int idColumnIndex = -1;
				for (int col = 0; col < mHeaders.Count; col++)
				{
					string headerName = mHeaders[col] == null ? string.Empty : mHeaders[col].Trim();
					if (string.Equals(headerName, "ID", StringComparison.OrdinalIgnoreCase) ||
						string.Equals(headerName, "Id", StringComparison.OrdinalIgnoreCase))
					{
						idColumnIndex = col;
						break;
					}
				}

				mRows = new List<List<object>>();
				for (int row = dataStartRowIndex; row <= rowCount; row++)
				{
					// ID判空要在类型转换前做，避免空ID被转换成0后误判为有效
					if (idColumnIndex >= 0)
					{
						int idSourceCol = validColumnIndexes[idColumnIndex];
						string idRawText = sheet.Cells[row, idSourceCol].Text;
						if (string.IsNullOrWhiteSpace(idRawText))
						{
							continue;
						}
					}

					List<object> rowData = new List<object>(validColumnIndexes.Count);
					for (int i = 0; i < validColumnIndexes.Count; i++)
					{
						int sourceCol = validColumnIndexes[i];
						string cellText = sheet.Cells[row, sourceCol].Text;
						rowData.Add(ParseValueByType(cellText, mFieldTypes[i], row, sourceCol));
					}

					mRows.Add(rowData);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogError("ExcelUtility读取失败: " + ex.Message + "\n路径: " + excelFile);
		}
	}

	/// <summary>
	/// 转换为实体类列表
	/// </summary>
	public List<T> ConvertToList<T>()
	{
		if (!HasData())
			return null;

		int colCount = mHeaders.Count;
		List<T> list = new List<T>();

		for (int i = 0; i < mRows.Count; i++)
		{
			Type t = typeof(T);
			ConstructorInfo ct = t.GetConstructor(System.Type.EmptyTypes);
			T target = (T)ct.Invoke(null);
			for (int j = 0; j < colCount; j++)
			{
				string field = mHeaders[j];
				object value = mRows[i][j];
				SetTargetProperty(target, field, value);
			}

			list.Add(target);
		}

		return list;
	}

	/// <summary>
	/// 转换为Json
	/// </summary>
	/// <param name="JsonPath">Json文件路径</param>
	/// <param name="Header">表头行数</param>
	public void ConvertToJson(string JsonPath, Encoding encoding)
	{
		if (!HasData())
			return;

		int colCount = mHeaders.Count;
		List<Dictionary<string, object>> table = new List<Dictionary<string, object>>();

		for (int i = 0; i < mRows.Count; i++)
		{
			Dictionary<string, object> row = new Dictionary<string, object>();
			for (int j = 0; j < colCount; j++)
			{
				string field = mHeaders[j];
				row[field] = mRows[i][j];
			}

			table.Add(row);
		}

		StringBuilder sb = new StringBuilder();
		sb.Append("[\n");
		for (int i = 0; i < table.Count; i++)
		{
			sb.Append("  ");
			sb.Append(JsonConvert.SerializeObject(table[i], Formatting.None));
			if (i < table.Count - 1)
				sb.Append(",");
			sb.Append("\n");
		}
		sb.Append("]");
		string json = sb.ToString();
		using (FileStream fileStream = new FileStream(JsonPath, FileMode.Create, FileAccess.Write))
		using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
		{
			textWriter.Write(json);
		}
	}

	/// <summary>
	/// 转换为CSV
	/// </summary>
	public void ConvertToCSV(string CSVPath, Encoding encoding)
	{
		if (mHeaders == null || mHeaders.Count < 1)
			return;

		int colCount = mHeaders.Count;
		StringBuilder stringBuilder = new StringBuilder();

		for (int j = 0; j < colCount; j++)
		{
			stringBuilder.Append(mHeaders[j] + ",");
		}
		stringBuilder.Append("\r\n");

		if (mRows != null)
		{
			for (int i = 0; i < mRows.Count; i++)
			{
				for (int j = 0; j < colCount; j++)
				{
					stringBuilder.Append((mRows[i][j] == null ? string.Empty : mRows[i][j].ToString()) + ",");
				}
				stringBuilder.Append("\r\n");
			}
		}

		using (FileStream fileStream = new FileStream(CSVPath, FileMode.Create, FileAccess.Write))
		using (TextWriter textWriter = new StreamWriter(fileStream, encoding))
		{
			textWriter.Write(stringBuilder.ToString());
		}
	}

	/// <summary>
	/// 导出为Xml
	/// </summary>
	public void ConvertToXml(string XmlFile)
	{
		if (!HasData())
			return;

		int colCount = mHeaders.Count;
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
		stringBuilder.Append("\r\n");
		stringBuilder.Append("<Table>");
		stringBuilder.Append("\r\n");

		for (int i = 0; i < mRows.Count; i++)
		{
			stringBuilder.Append("  <Row>");
			stringBuilder.Append("\r\n");
			for (int j = 0; j < colCount; j++)
			{
				stringBuilder.Append("   <" + mHeaders[j] + ">");
				stringBuilder.Append(mRows[i][j] == null ? string.Empty : mRows[i][j].ToString());
				stringBuilder.Append("</" + mHeaders[j] + ">");
				stringBuilder.Append("\r\n");
			}
			stringBuilder.Append("  </Row>");
			stringBuilder.Append("\r\n");
		}

		stringBuilder.Append("</Table>");
		using (FileStream fileStream = new FileStream(XmlFile, FileMode.Create, FileAccess.Write))
		using (TextWriter textWriter = new StreamWriter(fileStream, Encoding.GetEncoding("utf-8")))
		{
			textWriter.Write(stringBuilder.ToString());
		}
	}

	private bool HasData()
	{
		return mHeaders != null && mHeaders.Count > 0 && mRows != null && mRows.Count > 0;
	}

	private object ParseValueByType(string rawValue, string fieldType, int row, int col)
	{
		string value = rawValue == null ? string.Empty : rawValue.Trim();
		switch (fieldType)
		{
			case "int":
				int intValue;
				if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
				{
					return intValue;
				}
				return 0;
			case "float":
				float floatValue;
				if (float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out floatValue))
				{
					return floatValue;
				}
				return 0f;
			case "bool":
				bool boolValue;
				if (bool.TryParse(value, out boolValue))
				{
					return boolValue;
				}
				if (value == "1") return true;
				if (value == "0") return false;
				return false;
			case "string":
			default:
				return value;
		}
	}

	/// <summary>
	/// 设置目标实例的属性
	/// </summary>
	private void SetTargetProperty(object target, string propertyName, object propertyValue)
	{
		Type mType = target.GetType();
		PropertyInfo[] mPropertys = mType.GetProperties();
		foreach (PropertyInfo property in mPropertys)
		{
			if (property.Name == propertyName)
			{
				property.SetValue(target, Convert.ChangeType(propertyValue, property.PropertyType), null);
			}
		}
	}
}

