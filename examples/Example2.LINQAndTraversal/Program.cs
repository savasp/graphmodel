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

using Cvoya.Graph.Model;
using Cvoya.Graph.Provider.Neo4j;
using Cvoya.Graph.Provider.Neo4j.Linq;
using Neo4j.Driver;

// Example 2: LINQ and Traversal
// Demonstrates LINQ querying and graph traversal with depth control

Console.WriteLine("=== Example 2: LINQ and Traversal ===\n");

const string databaseName = "example2";

// ==== SETUP a new database ====
Console.WriteLine("0. Setting up a new database...");
var driver = GraphDatabase.Driver("bolt://localhost:7687", AuthTokens.Basic("neo4j", "password"));
await using (var session = driver.AsyncSession())
{
    await session.RunAsync($"CREATE OR REPLACE DATABASE {databaseName}");
}

Console.WriteLine($"✓ Created database: {databaseName}");

// We start with the Neo4j Graph Provider here

// Create graph instance with Neo4j provider
var graph = new Neo4jGraphProvider("bolt://localhost:7687", "neo4j", "password", databaseName, null);



try
{
    // ==== SETUP: Create a social network ====
    Console.WriteLine("1. Creating social network...");

    // Create people
    var alice = new Person { Name = "Alice", Age = 30, City = "New York" };
    var bob = new Person { Name = "Bob", Age = 28, City = "San Francisco" };
    var charlie = new Person { Name = "Charlie", Age = 35, City = "New York" };
    var diana = new Person { Name = "Diana", Age = 32, City = "San Francisco" };
    var eve = new Person { Name = "Eve", Age = 29, City = "New York" };

    await graph.CreateNode(alice);
    await graph.CreateNode(bob);
    await graph.CreateNode(charlie);
    await graph.CreateNode(diana);
    await graph.CreateNode(eve);

    // Create relationships
    await graph.CreateRelationship(new Knows { SourceId = alice.Id, TargetId = bob.Id, Since = DateTime.UtcNow.AddYears(-5) });
    await graph.CreateRelationship(new Knows { SourceId = alice.Id, TargetId = charlie.Id, Since = DateTime.UtcNow.AddYears(-3) });
    await graph.CreateRelationship(new Knows { SourceId = bob.Id, TargetId = diana.Id, Since = DateTime.UtcNow.AddYears(-2) });
    await graph.CreateRelationship(new Knows { SourceId = charlie.Id, TargetId = eve.Id, Since = DateTime.UtcNow.AddYears(-1) });
    await graph.CreateRelationship(new Knows { SourceId = diana.Id, TargetId = eve.Id, Since = DateTime.UtcNow.AddMonths(-6) });

    Console.WriteLine("✓ Created 5 people and their relationships\n");

    // ==== LINQ QUERIES ====
    Console.WriteLine("2. LINQ Query Examples...");

    // Basic filtering
    var newYorkers = graph.Nodes<Person>()
        .Where(p => p.City == "New York")
        .OrderBy(p => p.Name)
        .ToList();

    Console.WriteLine($"People in New York ({newYorkers.Count}):");
    foreach (var person in newYorkers)
    {
        Console.WriteLine($"  - {person.Name}, Age: {person.Age}");
    }

    // Age range query
    var youngPeople = graph.Nodes<Person>()
        .Where(p => p.Age >= 28 && p.Age <= 32)
        .ToList();

    Console.WriteLine($"\nPeople aged 28-32 ({youngPeople.Count}):");
    foreach (var person in youngPeople)
    {
        Console.WriteLine($"  - {person.Name}, Age: {person.Age}");
    }

    // ==== TRAVERSAL WITH DEPTH CONTROL ====
    Console.WriteLine("\n3. Graph Traversal Examples...");

    // Depth 0: Just the node
    alice = await graph.GetNode<Person>(alice.Id);
    Console.WriteLine($"\nAlice with depth 0:");
    Console.WriteLine($"  - Name: {alice.Name}");

    // Depth 1: Node with immediate relationships
    var aliceKnowsDepth1 = await graph.Nodes<Person>()
        .Where(p => p.Id == alice.Id)
        .TraversePath<Person, Knows, Person>()
        .ToListAsync();

    Console.WriteLine($"\nAlice with depth 1:");
    Console.WriteLine($"  - Name: {alice.Name}");
    Console.WriteLine($"  - Knows: {string.Join(", ", aliceKnowsDepth1.Select(k => k.Target.Name ?? "Unknown"))}");

    // Depth 2: Two levels of relationships
    var aliceKnowsDepth2 = await graph.Nodes<Person>()
        .Where(p => p.Id == alice.Id)
        .TraversePath<Person, Knows, Person>()
        .WithDepth(2)
        .ToListAsync();

    Console.WriteLine($"\nAlice with depth 2:");
    Console.WriteLine($"  - Name: {alice.Name}");
    foreach (var knows in aliceKnowsDepth2)
    {
        Console.WriteLine($"  - Knows: {knows?.Target?.Name ?? "Unknown"}");
    }

    // ==== COMPLEX LINQ QUERIES ====
    Console.WriteLine("\n4. Complex LINQ Queries...");

    // Find people who know someone in San Francisco
    var peopleWhoKnowSomeoneInSF = await graph.Nodes<Person>()
        .TraversePath<Person, Knows, Person>()
        .Where(path => path.Target != null && path.Target.City == "San Francisco")
        .Select(path => path.Source!)
        .Distinct()
        .ToListAsync();

    Console.WriteLine($"\nPeople who know someone in San Francisco:");
    foreach (var person in peopleWhoKnowSomeoneInSF)
    {
        Console.WriteLine($"  - {person.Name} (Age: {person.Age}, City: {person.City})");
    }

    // Find mutual connections
    Console.WriteLine("\n5. Finding mutual connections...");

    // Get all people and their connections
    // We need to work around the LINQ provider limitations
    var allPaths = await graph.Nodes<Person>()
        .TraversePath<Person, Knows, Person>()
        .ToListAsync();

    // Group by source person in memory
    var peopleWithConnections = allPaths
        .Where(path => path.Source != null && path.Target != null)
        .GroupBy(path => path.Source!.Id)
        .Select(g => new PersonConnections(
            g.Key,
            g.Select(path => path.Target!.Id).ToList()
        ))
        .ToList();

    // Build a lookup dictionary
    var connectionsLookup = peopleWithConnections.ToDictionary(x => x.PersonId, x => x.KnowsIds);

    // Get all people for name lookup
    var allPeople = await graph.Nodes<Person>().ToListAsync();
    var peopleLookup = allPeople.ToDictionary(p => p.Id, p => p.Name);

    var processedPairs = new HashSet<(string, string)>();

    // Find mutual connections
    foreach (var person1 in allPeople)
    {
        foreach (var person2 in allPeople.Where(p => p.Id != person1.Id))
        {
            // Create a normalized pair (smaller ID first) to avoid duplicates
            var pair = string.Compare(person1.Id, person2.Id) < 0
                ? (person1.Id, person2.Id)
                : (person2.Id, person1.Id);

            if (processedPairs.Contains(pair))
                continue;

            processedPairs.Add(pair);

            if (connectionsLookup.TryGetValue(person1.Id, out var person1Knows) &&
                connectionsLookup.TryGetValue(person2.Id, out var person2Knows))
            {
                var mutualIds = person1Knows.Intersect(person2Knows).ToList();

                if (mutualIds.Any())
                {
                    var mutualNames = mutualIds
                        .Where(id => peopleLookup.ContainsKey(id))
                        .Select(id => peopleLookup[id]);

                    Console.WriteLine($"  - {person1.Name} and {person2.Name} both know: {string.Join(", ", mutualNames)}");
                }
            }
        }
    }

    Console.WriteLine("\n=== Example 2 Complete ===");
    Console.WriteLine("This example demonstrated:");
    Console.WriteLine("• LINQ queries with filtering, ordering, and projection");
    Console.WriteLine("• Graph traversal with depth control");
    Console.WriteLine("• Complex queries involving relationships");
    Console.WriteLine("• Finding patterns in the graph");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
    if (ex.InnerException != null)
    {
        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
        Console.WriteLine(ex.InnerException.StackTrace);
    }
    Console.WriteLine("Make sure Neo4j is running on localhost:7687 with username 'neo4j' and password 'password'");
}
finally
{
    await graph.DisposeAsync();
    await using (var session = driver.AsyncSession())
    {
        await session.RunAsync($"DROP DATABASE {databaseName}");
    }
    await driver.DisposeAsync();
}
