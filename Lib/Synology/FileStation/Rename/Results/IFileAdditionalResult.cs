﻿using Synology.FileStation.Common.Results;

namespace Synology.FileStation.Rename.Results
{
    /// <summary>
    /// File additional result.
    /// </summary>
    public interface IFileAdditionalResult : ITaskFileAdditionalResult<IFileOwnerResult, IFileTimeResult, IFilePermResult, IFilePermAclResult>
    {
    }
}

