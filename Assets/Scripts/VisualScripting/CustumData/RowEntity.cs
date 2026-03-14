using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data
{
    /// <summary>
    /// CSV의 한 행(Row) 데이터를 저장하는 DTO 클래스
    /// </summary>
    [Serializable]
    public class RowEntity
    {
        // 컬럼명(Key)을 기준으로 파싱된 데이터를 object로 저장
        private Dictionary<string, object> _dataMap = new Dictionary<string, object>();

        /// <summary>
        /// DAO에서 파싱된 데이터를 DTO에 삽입할 때 사용
        /// </summary>
        public void SetField(string columnName, object value)
        {
            _dataMap[columnName] = value;
        }

        /// <summary>
        /// 원하는 타입(T)으로 데이터를 안전하게 추출
        /// </summary>
        /// <typeparam name="T">추출할 데이터 타입 (int, float, string, bool 등)</typeparam>
        /// <param name="columnName">구글 시트의 헤더 명칭</param>
        public T Get<T>(string columnName)
        {
            if (!_dataMap.TryGetValue(columnName, out object value))
            {
                Debug.LogWarning($"[RowEntity] 필드를 찾을 수 없습니다: {columnName}");
                return default;
            }

            if (value == null) return default;

            try
            {
                // 기본 타입 간의 유연한 변환을 위해 System.Convert 사용
                // 예: double로 저장된 값을 float으로 호출할 때의 예외 방지
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                Debug.LogError($"[RowEntity] '{columnName}' 형변환 오류 ({value.GetType()} -> {typeof(T)}): {e.Message}");
                return default;
            }
        }

        // 전체 데이터 컬럼 확인용
        public IEnumerable<string> GetColumns() => _dataMap.Keys;
    }
}