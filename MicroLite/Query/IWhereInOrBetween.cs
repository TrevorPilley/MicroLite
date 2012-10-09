﻿// -----------------------------------------------------------------------
// <copyright file="IWhereInOrBetween.cs" company="MicroLite">
// Copyright 2012 Trevor Pilley
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// </copyright>
// -----------------------------------------------------------------------
namespace MicroLite.Query
{
    /// <summary>
    /// The interface which specifies the where in method in the fluent sql builder syntax.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "WhereIn", Justification = "In this case, it means where in.")]
    public interface IWhereInOrBetween : IHideObjectMethods
    {
        /// <summary>
        /// Uses the specified arguments to filter the column.
        /// </summary>
        /// <param name="lower">The inclusive lower value.</param>
        /// <param name="upper">The inclusive upper value.</param>
        /// <returns>The next step in the fluent sql builder.</returns>
        IAndOrOrderBy Between(object lower, object upper);

        /// <summary>
        /// Uses the specified arguments to filter the column.
        /// </summary>
        /// <param name="args">The arguments to filter the column.</param>
        /// <returns>The next step in the fluent sql builder.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "In", Justification = "The method is to specify an In list.")]
        IAndOrOrderBy In(params object[] args);

        /// <summary>
        /// Uses the specified SqlQuery as a sub query to filter the column.
        /// </summary>
        /// <param name="subQuery">The sub query.</param>
        /// <returns>The next step in the fluent sql builder.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "In", Justification = "The method is to specify an In list.")]
        IAndOrOrderBy In(SqlQuery subQuery);
    }
}