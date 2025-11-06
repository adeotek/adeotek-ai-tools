namespace PostgresMcp.Services;

public static class DbQueries
{
    public const string GetTablesSql = """
       SELECT
           t.table_schema,
           t.table_name,
           pg_stat.n_live_tup as row_count,
           pg_total_relation_size(quote_ident(t.table_schema) || '.' || quote_ident(t.table_name)) as size_bytes,
           obj_description((quote_ident(t.table_schema) || '.' || quote_ident(t.table_name))::regclass, 'pg_class') as table_comment
       FROM information_schema.tables t
       LEFT JOIN pg_stat_user_tables pg_stat
           ON t.table_schema = pg_stat.schemaname
           AND t.table_name = pg_stat.relname
       WHERE t.table_type = 'BASE TABLE'
           AND t.table_schema NOT IN ('pg_catalog', 'information_schema')
           AND (@schemaFilter IS NULL OR t.table_schema = @schemaFilter)
       ORDER BY t.table_schema, t.table_name
       """;

    public const string GetColumnsSql = """
       SELECT
           c.column_name,
           c.data_type,
           c.is_nullable,
           c.column_default,
           c.character_maximum_length,
           c.numeric_precision,
           c.numeric_scale,
           c.ordinal_position,
           c.is_identity,
           col_description((quote_ident(c.table_schema) || '.' || quote_ident(c.table_name))::regclass, c.ordinal_position) as column_comment
       FROM information_schema.columns c
       WHERE c.table_schema = @schemaName
           AND c.table_name = @tableName
       ORDER BY c.ordinal_position
       """;

    public const string GetPrimaryKeySql = """
       SELECT
           tc.constraint_name,
           string_agg(kcu.column_name, ',' ORDER BY kcu.ordinal_position) as columns
       FROM information_schema.table_constraints tc
       JOIN information_schema.key_column_usage kcu
           ON tc.constraint_name = kcu.constraint_name
           AND tc.table_schema = kcu.table_schema
       WHERE tc.constraint_type = 'PRIMARY KEY'
           AND tc.table_schema = @schemaName
           AND tc.table_name = @tableName
       GROUP BY tc.constraint_name
       """;

    public  const string GetForeignKeysSql = """
       SELECT
           tc.constraint_name,
           kcu.column_name,
           ccu.table_schema AS referenced_schema,
           ccu.table_name AS referenced_table,
           ccu.column_name AS referenced_column,
           rc.delete_rule,
           rc.update_rule
       FROM information_schema.table_constraints tc
       JOIN information_schema.key_column_usage kcu
           ON tc.constraint_name = kcu.constraint_name
           AND tc.table_schema = kcu.table_schema
       JOIN information_schema.constraint_column_usage ccu
           ON ccu.constraint_name = tc.constraint_name
           AND ccu.table_schema = tc.table_schema
       JOIN information_schema.referential_constraints rc
           ON rc.constraint_name = tc.constraint_name
           AND rc.constraint_schema = tc.table_schema
       WHERE tc.constraint_type = 'FOREIGN KEY'
           AND tc.table_schema = @schemaName
           AND tc.table_name = @tableName
       """;

    public const string GetIndexesSql = """
      SELECT
          i.relname AS index_name,
          array_agg(a.attname ORDER BY x.ordinality) AS columns,
          ix.indisunique AS is_unique,
          am.amname AS index_type,
          ix.indisprimary AS is_primary
      FROM pg_index ix
      JOIN pg_class t ON t.oid = ix.indrelid
      JOIN pg_class i ON i.oid = ix.indexrelid
      JOIN pg_namespace n ON n.oid = t.relnamespace
      JOIN pg_am am ON i.relam = am.oid
      CROSS JOIN LATERAL unnest(ix.indkey) WITH ORDINALITY AS x(attnum, ordinality)
      JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = x.attnum
      WHERE n.nspname = @schemaName
          AND t.relname = @tableName
      GROUP BY i.relname, ix.indisunique, am.amname, ix.indisprimary
      """;

    public const string GetViewsSql = """
      SELECT
          table_schema,
          table_name,
          view_definition
      FROM information_schema.views
      WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
          AND (@schemaFilter IS NULL OR table_schema = @schemaFilter)
      ORDER BY table_schema, table_name
      """;

    public const string GetRelationshipsSql = """
      SELECT
          tc.table_schema || '.' || tc.table_name AS source_table,
          ccu.table_schema || '.' || ccu.table_name AS target_table,
          tc.constraint_name
      FROM information_schema.table_constraints tc
      JOIN information_schema.constraint_column_usage ccu
          ON ccu.constraint_name = tc.constraint_name
          AND ccu.table_schema = tc.table_schema
      WHERE tc.constraint_type = 'FOREIGN KEY'
          AND tc.table_schema NOT IN ('pg_catalog', 'information_schema')
          AND (@schemaFilter IS NULL OR tc.table_schema = @schemaFilter)
      """;
}
