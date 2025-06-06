// Copyright 2025 Savas Parastatidis
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections;
using System.Reflection;
using Cvoya.Graph.Model;
using Microsoft.Extensions.Logging;
using Neo4j.Driver;

namespace Cvoya.Graph.Provider.Neo4j.Schema;

/// <summary>
/// Manages Neo4j constraints to ensure data integrity.
/// </summary>
internal class Neo4jConstraintManager
{
    private readonly HashSet<string> _constrainedLabels = [];
    private readonly object _constraintLock = new();
    private bool _constraintsLoaded = false;
    private readonly string _databaseName;
    private readonly IDriver _driver;
    private readonly Microsoft.Extensions.Logging.ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the Neo4jConstraintManager class.
    /// </summary>
    /// <param name="driver">The Neo4j driver</param>
    /// <param name="databaseName">The database name</param>
    /// <param name="logger">Optional logger</param>
    public Neo4jConstraintManager(IDriver driver, string databaseName, Microsoft.Extensions.Logging.ILogger? logger = null)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
        _logger = logger;
    }

    /// <summary>
    /// Ensures that all necessary constraints exist for the specified label.
    /// </summary>
    /// <param name="label">The node label</param>
    /// <param name="properties">The properties to constrain</param>
    public async Task EnsureConstraintsForLabel(string label, IEnumerable<PropertyInfo> properties)
    {
        await LoadExistingConstraints();

        lock (_constraintLock)
        {
            if (_constrainedLabels.Contains(label))
                return;
            _constrainedLabels.Add(label);
        }

        // Create the session and transaction outside the provider
        var session = _driver.AsyncSession(builder => builder.WithDatabase(_databaseName));
        await using var _ = session;

        var tx = await session.BeginTransactionAsync();
        await using var __ = tx;

        // Always add unique constraint for the identifier property
        var cypher = $"CREATE CONSTRAINT IF NOT EXISTS FOR (n:`{label}`) REQUIRE n.{nameof(Model.INode.Id)} IS UNIQUE";
        await tx.RunAsync(cypher);

        foreach (var prop in properties)
        {
            if (prop.Name == nameof(Model.INode.Id)) continue;
            var name = prop.GetCustomAttribute<PropertyAttribute>()?.Label ?? prop.Name;
            var propCypher = $"CREATE CONSTRAINT IF NOT EXISTS FOR (n:`{label}`) REQUIRE n.{name} IS NOT NULL";
            await tx.RunAsync(propCypher);
        }

        await tx.CommitAsync();
    }

    /// <summary>
    /// Loads the existing constraints from Neo4j
    /// </summary>
    public async Task LoadExistingConstraints()
    {
        if (_constraintsLoaded) return;
        lock (_constraintLock)
        {
            if (_constraintsLoaded) return;
            _constraintsLoaded = true;
        }

        var cypher = "SHOW CONSTRAINTS";
        var session = _driver.AsyncSession(builder => builder.WithDatabase(_databaseName));
        await using var _ = session;

        var tx = await session.BeginTransactionAsync();
        await using var __ = tx;

        try
        {
            var cursor = await tx.RunAsync(cypher);
            while (await cursor.FetchAsync())
            {
                var record = cursor.Current;
                if (record.Values.TryGetValue("labelsOrTypes", out var labelsOrTypesObj) &&
                    labelsOrTypesObj is IEnumerable<object> labelsOrTypes)
                {
                    foreach (var label in labelsOrTypes)
                    {
                        if (label is string s)
                        {
                            _constrainedLabels.Add(s);
                        }
                    }
                }
            }

            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load existing constraints from Neo4j.");
            throw new GraphException("Failed to load existing constraints from Neo4j.", ex);
        }
    }
}