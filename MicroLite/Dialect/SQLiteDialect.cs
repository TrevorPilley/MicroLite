﻿// -----------------------------------------------------------------------
// <copyright file="SQLiteDialect.cs" company="MicroLite">
// Copyright 2012 - 2014 Project Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// </copyright>
// -----------------------------------------------------------------------
namespace MicroLite.Dialect
{
    using System;
    using System.Text;
    using MicroLite.Mapping;

    /// <summary>
    /// The implementation of <see cref="ISqlDialect" /> for SQLite.
    /// </summary>
    internal sealed class SQLiteDialect : SqlDialect
    {
        private static readonly SqlQuery selectIdentityQuery = new SqlQuery("SELECT last_insert_rowid()");

        /// <summary>
        /// Initialises a new instance of the <see cref="SQLiteDialect"/> class.
        /// </summary>
        internal SQLiteDialect()
            : base(SQLiteCharacters.Instance)
        {
        }

        public override bool SupportsIdentity
        {
            get
            {
                return true;
            }
        }

        public override SqlQuery BuildSelectInsertIdSqlQuery(IObjectInfo objectInfo)
        {
            return selectIdentityQuery;
        }

        /// <summary>
        /// Creates an SqlQuery to page the records which would be returned by the specified SqlQuery based upon the paging options.
        /// </summary>
        /// <param name="sqlQuery">The SQL query.</param>
        /// <param name="pagingOptions">The paging options.</param>
        /// <returns>
        /// A <see cref="T:MicroLite.SqlQuery" /> to return the paged results of the specified query.
        /// </returns>
        public override SqlQuery PageQuery(SqlQuery sqlQuery, PagingOptions pagingOptions)
        {
            if (sqlQuery == null)
            {
                throw new ArgumentNullException("sqlQuery");
            }

            var arguments = new object[sqlQuery.Arguments.Count + 2];
            Array.Copy(sqlQuery.ArgumentsArray, 0, arguments, 0, sqlQuery.Arguments.Count);
            arguments[arguments.Length - 2] = pagingOptions.Offset;
            arguments[arguments.Length - 1] = pagingOptions.Count;

            var stringBuilder = new StringBuilder(sqlQuery.CommandText)
                .Replace(Environment.NewLine, string.Empty)
                .Append(" LIMIT ")
                .Append(this.SqlCharacters.GetParameterName(arguments.Length - 2))
                .Append(',')
                .Append(this.SqlCharacters.GetParameterName(arguments.Length - 1));

            return new SqlQuery(stringBuilder.ToString(), arguments);
        }
    }
}