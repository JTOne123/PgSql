﻿using System;
using System.Collections.Generic;
using System.Linq;
using doob.PgSql.Exceptions;
using Npgsql;

namespace doob.PgSql
{
    public class Database
    {

        private ConnectionString _connectionString;

        public ConnectionString GetConnectionString() => _connectionString.Clone();

        //public Database() : this(ConnectionString.Build()) { }

        //public Database(string connectionString) : this(ConnectionString.Build(connectionString)) { }

        //public Database(ConnectionStringBuilder connectionBuilder) : this(connectionBuilder.GetConnection()) { }

        public Database(ConnectionString connection)
        {
            var missing = new List<string>();

            if (String.IsNullOrWhiteSpace(connection.DatabaseName))
                missing.Add(nameof(connection.DatabaseName));

            if (missing.Any())
                throw new ArgumentNullException($"Missing Options to connect: '{String.Join(", ", missing)}'");

            _connectionString = ConnectionString.Build(connection);
        }

        public Server GetServer()
        {
            return new Server(GetConnectionString());
        }

        

        public bool Exists()
        {
            return new Server(GetConnectionString()).DatabaseExists(GetConnectionString().DatabaseName);
        }
        public Database EnsureExists()
        {
            GetServer().DatabaseExists(GetConnectionString().DatabaseName, true, false);
            return this;
        }
        public Database EnsureNotExists()
        {
            GetServer().DatabaseExists(GetConnectionString().DatabaseName, false, true);
            return this;
        }
        public Database CreateIfNotExists()
        {
            return new Server(GetConnectionString()).CreateDatabase(GetConnectionString().DatabaseName, false);
        }


        public void Drop()
        {
            GetServer().DropDatabase(GetConnectionString().DatabaseName);
        }
        public void Drop(bool force)
        {
            GetServer().DropDatabase(GetConnectionString().DatabaseName, force);
        }
        public void Drop(bool force, bool throwIfNotExists)
        {
            GetServer().DropDatabase(GetConnectionString().DatabaseName, force, throwIfNotExists);
        }


        public string[] SchemaList()
        {
            var l = new List<string>();
            foreach (var sch in new PgSqlExecuter(GetConnectionString()).ExecuteReader<Dictionary<string, string>>(SQLStatements.SchemaGetAll()))
            {
                l.Add(sch["schema_name"]);
            }
            return l.ToArray();
        }
        public bool SchemaExists(string schemaName)
        {
            return SchemaExists(schemaName, false);
        }
        public bool SchemaExists(string schemaName, bool throwIfNotExists)
        {
            return SchemaExists(schemaName, throwIfNotExists, false);
        }
        public bool SchemaExists(string schemaName, bool throwIfNotExists, bool throwIfAlreadyExists)
        {
            if (String.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentNullException(nameof(schemaName));


            var exists = new PgSqlExecuter(GetConnectionString()).ExecuteScalar<bool>(SQLStatements.SchemaExists(schemaName));

            if (!exists && throwIfNotExists)
                throw new SchemaDoesntExistsException(schemaName);

            if (exists && throwIfAlreadyExists)
                throw new SchemaAlreadyExistsException(schemaName);

            return exists;
        }


        public Schema GetSchema(string schemaName)
        {
            var connstr = ConnectionString.Build(GetConnectionString()).WithSchema(schemaName);
            return new Schema(connstr);
        }

        public Schema CreateSchema(string schemaName)
        {
            return CreateSchema(schemaName, true);
        }
        public Schema CreateSchema(string schemaName, bool throwIfAlreadyExists)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(schemaName))
                    throw new ArgumentNullException(nameof(schemaName));

                //schemaName = schemaName.ToLower();

                var sqlCommand = new PgSqlCommand();
                sqlCommand.AppendCommand(SQLStatements.SchemaCreate(schemaName, throwIfAlreadyExists));
                var resp = new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);

                return GetSchema(schemaName);
            }
            catch (PostgresException pex)
            {
                if (pex.SqlState == "42P06")
                    throw new SchemaAlreadyExistsException(schemaName);

                throw;
            }

        }

        public Schema SchemaRename(string schemaName, string newSchemaName)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(schemaName))
                    throw new ArgumentNullException(nameof(schemaName));

                if (String.IsNullOrWhiteSpace(newSchemaName))
                    throw new ArgumentNullException(nameof(newSchemaName));

                var sqlCommand = new PgSqlCommand();
                sqlCommand.AppendCommand(SQLStatements.SchemaRename(schemaName, newSchemaName));
                var resp = new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);

                return GetSchema(newSchemaName);
            }
            catch (PostgresException pex)
            {

                if (pex.SqlState == "3F000")
                    throw new SchemaDoesntExistsException(schemaName);

                if (pex.SqlState == "42P06")
                    throw new SchemaAlreadyExistsException(newSchemaName);

                throw;
            }

        }

        public void SchemaDrop(string schemaName)
        {
            SchemaDrop(schemaName, false);
        }
        public void SchemaDrop(string schemaName, bool force)
        {
            SchemaDrop(schemaName, force, true);
        }
        public void SchemaDrop(string schemaName, bool force, bool throwIfNotExists)
        {
            if (String.IsNullOrWhiteSpace(schemaName))
                throw new ArgumentNullException(nameof(schemaName));

            try
            {
                var sqlCommand = new PgSqlCommand();
                sqlCommand.AppendCommand(SQLStatements.SchemaDrop(schemaName, force, throwIfNotExists));
                var resp = new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
            }
            catch (PostgresException pex)
            {
                if (pex.SqlState == "2BP01")
                    throw new DependentObjectsException(pex.Detail);

                if (pex.SqlState == "3F000")
                    throw new SchemaDoesntExistsException(schemaName);

                throw;
            }

        }




        #region Extensions
        public bool ExtensionExists(string extensionName)
        {
            if (String.IsNullOrWhiteSpace(extensionName))
                throw new ArgumentNullException(nameof(extensionName));

            return new PgSqlExecuter(GetConnectionString()).ExecuteScalar<bool>(SQLStatements.ExtensionExists(extensionName));
        }
        public void ExtensionCreate(string extensionName, bool throwIfAlreadyExists)
        {
            if (String.IsNullOrWhiteSpace(extensionName))
                throw new ArgumentNullException(nameof(extensionName));

            var sqlCommand = new PgSqlCommand();
            sqlCommand.AppendCommand(SQLStatements.ExtensionCreate(extensionName, throwIfAlreadyExists));
            new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
        }
        public void ExtensionDrop(string extensionName, bool throwIfNotExists)
        {
            if (String.IsNullOrWhiteSpace(extensionName))
                throw new ArgumentNullException(nameof(extensionName));

            var sqlCommand = new PgSqlCommand();
            sqlCommand.AppendCommand(SQLStatements.ExtensionDrop(extensionName, throwIfNotExists));
            new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
        }
        public Dictionary<string, string>[] ExtensionList()
        {
            return new PgSqlExecuter(GetConnectionString()).ExecuteReader<Dictionary<string, string>>(SQLStatements.ExtensionList())?.ToArray();
        }
        #endregion

        #region Domain
        public bool DomainExists(string domainName)
        {
            if (String.IsNullOrWhiteSpace(domainName))
                throw new ArgumentNullException(nameof(domainName));
            return new PgSqlExecuter(GetConnectionString()).ExecuteScalar<bool>(SQLStatements.DomainExists(domainName));
        }
        public void DomainCreate(string domainName, string postgresTypeName, bool throwIfExists)
        {
            if (String.IsNullOrWhiteSpace(domainName))
                throw new ArgumentNullException(nameof(domainName));

            var sqlCommand = new PgSqlCommand();
            sqlCommand.AppendCommand(SQLStatements.DomainCreate(domainName, postgresTypeName));
            if (!throwIfExists)
            {
                if (!DomainExists(domainName))
                {
                    new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
                }
            }
            else
            {
                new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
            }
        }
        public void DomainDrop(string domainName, bool throwIfNotExists)
        {
            if (String.IsNullOrWhiteSpace(domainName))
                throw new ArgumentNullException(nameof(domainName));

            var sqlCommand = new PgSqlCommand();
            sqlCommand.AppendCommand(SQLStatements.DomainDrop(domainName, throwIfNotExists));
            new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(sqlCommand);
        }
        public IEnumerable<Dictionary<string, string>> DomainList()
        {
            return new PgSqlExecuter(GetConnectionString()).ExecuteReader<Dictionary<string, string>>(SQLStatements.DomainList());
        }
        #endregion


        #region Function

        public bool FunctionExists(string functionName)
        {
            var command = $"SELECT EXISTS(SELECT 1 FROM information_schema.routines WHERE routines.specific_schema = 'public' AND routines.routine_name = '{functionName}');";
            return new PgSqlExecuter(GetConnectionString()).ExecuteScalar<bool>(command);
        }

        #endregion

        #region Triggers

        public bool EventTriggerExists(string triggerName)
        {
            var command = $"SELECT EXISTS(select 1 from pg_event_trigger where evtname = '{triggerName}');";
            return new PgSqlExecuter(GetConnectionString()).ExecuteScalar<bool>(command);
        }

        public void EventTriggerDrop(string triggerName, bool force)
        {
            var command = $"DROP EVENT TRIGGER IF EXISTS \"{triggerName}\"";

            if (force)
                command = $"{command} CASCADE";

            new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(command);
        }

        public void RegisterEventTrigger(bool overwriteIfExists = false)
        {
            var functionName = "XA-Notify-DBEvent";

            if (!FunctionExists(functionName) || overwriteIfExists)
            {
                var function1 = $@"
CREATE OR REPLACE FUNCTION ""{functionName}""() RETURNS event_trigger AS $$
DECLARE
    r RECORD;
    notification json;
    type text;
BEGIN
    
    FOR r IN SELECT * FROM pg_event_trigger_ddl_commands() LOOP

        notification= json_build_object(
            'action', upper(r.command_tag),
            'schema', r.schema_name,
            'identity', r.object_identity,
            'type', upper(r.object_type)
        );

        PERFORM pg_notify('{functionName}', notification::text);

    END LOOP;
END;
$$ LANGUAGE plpgsql;
";
                new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(function1);
            }

            if (!FunctionExists($"{functionName}_dropped") || overwriteIfExists)
            {
                var function2 = $@"
CREATE OR REPLACE FUNCTION ""{functionName}_dropped""() RETURNS event_trigger AS $$
DECLARE
    r RECORD;
    notification json;
    action text;
BEGIN
    
    FOR r IN SELECT * FROM pg_event_trigger_dropped_objects() LOOP

        notification= json_build_object(
            'action', 'DROP ' || upper(r.object_type),
            'schema', r.schema_name,
            'identity', r.object_identity,
            'type', upper(r.object_type)
        );

        PERFORM pg_notify('{functionName}', notification::text);
    END LOOP;
END;
$$ LANGUAGE plpgsql;
";
                new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(function2);

            }

            var trigger1Exists = EventTriggerExists($"{functionName}_Trigger");

            if (!trigger1Exists || overwriteIfExists)
            {
                if(trigger1Exists)
                    EventTriggerDrop($"{functionName}_Trigger", true);

                var trigger1 = $"CREATE EVENT TRIGGER \"{functionName}_Trigger\" ON ddl_command_end EXECUTE PROCEDURE \"{functionName}\"();";
                new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(trigger1);
            }

            var trigger2Exists = EventTriggerExists($"{functionName}_Trigger_dropped");
            if (!trigger2Exists || overwriteIfExists)
            {
                if (trigger2Exists)
                    EventTriggerDrop($"{functionName}_Trigger_dropped", true);

                var trigger2 = $"CREATE EVENT TRIGGER \"{functionName}_Trigger_dropped\" ON sql_drop EXECUTE PROCEDURE \"{functionName}_dropped\"();";
                new PgSqlExecuter(GetConnectionString()).ExecuteNonQuery(trigger2);
            }

        }

        #endregion


    }
}
