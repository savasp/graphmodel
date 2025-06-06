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
/// Represents errors that occur specifically during graph transaction operations.
/// </summary>
/// <remarks>
/// This exception is thrown when transaction-specific errors occur, such as
/// during transaction creation, commitment, or rollback operations, or when
/// there are issues with transaction isolation or concurrency.
/// </remarks>
[Serializable]
public class GraphTransactionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GraphTransactionException"/> class 
    /// with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public GraphTransactionException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphTransactionException"/> class 
    /// with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, 
    /// or a null reference if no inner exception is specified.</param>
    /// <remarks>
    /// The <see cref="Exception.InnerException"/> property can be used to retrieve 
    /// the original exception that caused this exception.
    /// </remarks>
    public GraphTransactionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}