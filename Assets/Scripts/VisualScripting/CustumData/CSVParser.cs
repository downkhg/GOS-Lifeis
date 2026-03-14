using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public static class CSVParser
    {
        // 기획자가 시트 드롭다운으로 선택할 타입 명칭들과 파싱 로직 매핑
        public static object ParseValue(string typeName, string value)
        {
            if (string.IsNullOrEmpty(value)) return GetDefaultValue(typeName);

            try
            {
                switch (typeName.ToLower())
                {
                    case "int32": return int.Parse(value);
                    case "int64": return long.Parse(value);
                    case "float": return float.Parse(value);
                    case "double": return double.Parse(value);
                    case "string": return value;
                    case "bool": return bool.Parse(value);
                    case "ref": return value; // 참조는 일단 ID 문자열로 들고 있음
                    default:
                        Debug.LogError($"[CSVParser] 지원하지 않는 타입입니다: {typeName}");
                        return value;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CSVParser] 파싱 실패. 타입: {typeName}, 값: {value}, 에러: {e.Message}");
                return GetDefaultValue(typeName);
            }
        }

        private static object GetDefaultValue(string typeName)
        {
            switch (typeName.ToLower())
            {
                case "int32": case "int64": return 0;
                case "float": case "double": return 0.0f;
                case "bool": return false;
                default: return string.Empty;
            }
        }
    }
}