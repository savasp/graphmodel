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

namespace Cvoya.Graph.Model;

/// <summary>
/// Strategy for traversing the graph
/// </summary>
public enum TraversalStrategy
{
    /// <summary>Use depth-first traversal</summary>
    DepthFirst,

    /// <summary>Use breadth-first traversal</summary>
    BreadthFirst,

    /// <summary>Use shortest path algorithm</summary>
    ShortestPath,

    /// <summary>Use weighted shortest path algorithm</summary>
    WeightedShortestPath,

    /// <summary>Let the provider choose the optimal strategy</summary>
    Automatic
}