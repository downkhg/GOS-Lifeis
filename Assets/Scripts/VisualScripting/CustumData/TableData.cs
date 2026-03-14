using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Data
{
    public class TableData
    {
        // ID(string)를 키로 하여 RowEntity를 저장
        private Dictionary<string, RowEntity> _rows = new Dictionary<string, RowEntity>();

        public void AddRow(string id, RowEntity row) => _rows[id] = row;

        public RowEntity GetRow(string id)
        {
            if (!_rows.TryGetValue(id, out var row))
            {
                Debug.LogWarning($"[TableData] ID '{id}'를 찾을 수 없습니다.");
                return null;
            }
            return row;
        }

        public IEnumerable<RowEntity> AllRows => _rows.Values;
    }
}