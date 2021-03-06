﻿using doob.PgSql.Interfaces;

namespace doob.PgSql.Clauses
{
    public class LimitClauseNumber : ILimitClauseItem
    {
        public int Number { get; set; }

        private LimitClauseNumber()
        {

        }
        public LimitClauseNumber(int number)
        {
            Number = number;
        }

        public override string ToString()
        {
            return Number.ToString();
        }

        public PgSqlCommand GetSqlCommand(TableDefinition tableDefinition)
        {
            var sqlCommand = new PgSqlCommand();

            string num = Number.ToString();
            num = Number.ToString() == "0" ? "ALL" : Number.ToString();

            sqlCommand.AppendCommand(num);
            return sqlCommand;
        }
    }
}
