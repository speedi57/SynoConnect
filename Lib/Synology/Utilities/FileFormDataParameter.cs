﻿namespace Synology.Utilities
{
    /// <summary>
    /// A <seealso cref="FormParameter"/> specifying a file to be send as parameter of a post request
    /// </summary>
    public sealed class FileFormDataParameter : FormParameter
    {
        /// <summary>
        /// The file data to send
        /// </summary>
        public byte[] FileData { get; }

        /// <summary>
        /// Constructs a <seealso cref="FileFormDataParameter"/>
        /// </summary>
        /// <param name="parameterName">Name of the parameter</param>
        /// <param name="parameterValue">Name of the file</param>
        /// <param name="fileData">Byte array containing the data of the file</param>
        /// <param name="minVersion">The minimum version for this parameter</param>
        public FileFormDataParameter(string parameterName, string parameterValue, byte[] fileData, int minVersion = 1) : base(parameterName, parameterValue, minVersion)
        {
            FileData = fileData;
        }
    }
}
