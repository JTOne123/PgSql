﻿using System.Collections.Generic;

namespace doob.PgSql.Tables
{
    public enum ListenMode
    {
        ReferenceTablePrimaryKeys,
        ReferenceTableEntry,
        HistoryTableId,
        HistoryTableEntry
    }

    public class NotificationObject
    {
        public ListenMode Mode { get; set; }
        public string Schema { get; set; }
        public string Table { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}
