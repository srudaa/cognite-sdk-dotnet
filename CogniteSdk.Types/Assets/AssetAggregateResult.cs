// Copyright 2019 Cognite AS
// SPDX-License-Identifier: Apache-2.0

using CogniteSdk.Types.Common;

namespace CogniteSdk
{
    /// <summary>
    /// Aggregated metrics of the asset.
    /// </summary>
    public class AssetAggregateResult
    {
        /// <summary>
        /// Number of direct descendants for the asset.
        /// </summary>
        public int ChildCount { get; set; }

        /// <inheritdoc />
        public override string ToString() => Stringable.ToString(this);
    }
}
