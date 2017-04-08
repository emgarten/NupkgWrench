using System;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;

namespace NupkgWrench
{
    public static class ValidateCmdOptionsUtil
    {
        /// <summary>
        /// Throw if a required option does not exist.
        /// </summary>
        public static void VerifyRequiredOptions(params CommandOption[] required)
        {
            // Validate parameters
            foreach (var requiredOption in required)
            {
                if (!requiredOption.HasValue())
                {
                    throw new ArgumentException($"Missing required parameter --{requiredOption.LongName}.");
                }
            }
        }

        /// <summary>
        /// Throw if more than one of the options exist.
        /// </summary>
        public static void VerifyMutallyExclusiveOptions(params CommandOption[] exclusiveOptions)
        {
            var withValues = exclusiveOptions.Where(e => e.HasValue()).ToList();

            if (withValues.Count > 1)
            {
                throw new ArgumentException($"{string.Join(", ", withValues.Select(e => $"--{e.LongName}"))} may not be used together.");
            }
        }

        /// <summary>
        /// Verify at least one of the options exists.
        /// </summary>
        public static void VerifyOneOptionExists(params CommandOption[] options)
        {
            var withValues = options.Where(e => e.HasValue()).ToList();

            if (withValues.Count < 1)
            {
                throw new ArgumentException($"One of the following options must be specified: {string.Join(", ", options.Select(e => $"--{e.LongName}"))}");
            }
        }
    }
}
