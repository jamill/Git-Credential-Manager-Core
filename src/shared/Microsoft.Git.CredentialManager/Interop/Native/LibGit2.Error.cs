// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace Microsoft.Git.CredentialManager.Interop.Native
{
    public static partial class LibGit2
    {
        /// <summary>
        /// No error.
        /// </summary>
        public const int GIT_OK = 0;

        /// <summary>
        /// Generic error.
        /// </summary>
        public const int GIT_ERROR = -1;

        /// <summary>
        /// Requested object could not be found.
        /// </summary>
        public const int GIT_ENOTFOUND = -3;

        /// <summary>
        /// Object exists preventing operation.
        /// </summary>
        public const int GIT_EEXISTS = -4;

        /// <summary>
        /// More than one object matches.
        /// </summary>
        public const int GIT_EAMBIGUOUS = -5;

        /// <summary>
        /// Output buffer too short to hold data.
        /// </summary>
        public const int GIT_EBUFS = -6;

        /// <summary>
        /// GIT_EUSER is a special error that is never generated by libgit2
        /// code.  You can return it from a callback (e.g to stop an iteration)
        /// to know that it was generated by the callback and not by libgit2.
        /// </summary>
        public const int GIT_EUSER = -7;

        /// <summary>
        /// Operation not allowed on bare repository.
        /// </summary>
        public const int GIT_EBAREREPO = -8;

        /// <summary>
        /// HEAD refers to branch with no commits.
        /// </summary>
        public const int GIT_EUNBORNBRANCH = -9;

        /// <summary>
        /// Merge in progress prevented operation.
        /// </summary>
        public const int GIT_EUNMERGED = -10;

        /// <summary>
        /// Reference was not fast-forwardable.
        /// </summary>
        public const int GIT_ENONFASTFORWARD = -11;

        /// <summary>
        /// Name/ref spec was not in a valid format.
        /// </summary>
        public const int GIT_EINVALIDSPEC = -12;

        /// <summary>
        /// Checkout conflicts prevented operation.
        /// </summary>
        public const int GIT_ECONFLICT = -13;

        /// <summary>
        /// Lock file prevented operation.
        /// </summary>
        public const int GIT_ELOCKED = -14;

        /// <summary>
        /// Reference value does not match expected.
        /// </summary>
        public const int GIT_EMODIFIED = -15;

        /// <summary>
        /// Authentication error.
        /// </summary>
        public const int GIT_EAUTH = -16;

        /// <summary>
        /// Server certificate is invalid.
        /// </summary>
        public const int GIT_ECERTIFICATE = -17;

        /// <summary>
        /// Patch/merge has already been applied.
        /// </summary>
        public const int GIT_EAPPLIED = -18;

        /// <summary>
        /// The requested peel operation is not possible.
        /// </summary>
        public const int GIT_EPEEL = -19;

        /// <summary>
        /// Unexpected EOF.
        /// </summary>
        public const int GIT_EEOF = -20;

        /// <summary>
        /// Invalid operation or input.
        /// </summary>
        public const int GIT_EINVALID = -21;

        /// <summary>
        /// Uncommitted changes in index prevented operation.
        /// </summary>
        public const int GIT_EUNCOMMITTED = -22;

        /// <summary>
        /// The operation is not valid for a directory.
        /// </summary>
        public const int GIT_EDIRECTORY = -23;

        /// <summary>
        /// A merge conflict exists and cannot continue.
        /// </summary>
        public const int GIT_EMERGECONFLICT = -24;

        /// <summary>
        /// A user-configured callback refused to act.
        /// </summary>
        public const int GIT_PASSTHROUGH = -30;

        /// <summary>
        /// Signals end of iteration with iterator.
        /// </summary>
        public const int GIT_ITEROVER = -31;

        /// <summary>
        /// Internal only.
        /// </summary>
        public const int GIT_RETRY = -32;

        /// <summary>
        /// Hashsum mismatch in object.
        /// </summary>
        public const int GIT_EMISMATCH = -33;

        /// <summary>
        /// Unsaved changes in the index would be overwritten.
        /// </summary>
        public const int GIT_EINDEXDIRTY = -34;

        /// <summary>
        /// Patch application failed.
        /// </summary>
        public const int GIT_EAPPLYFAIL = -35;

        public static void ThrowIfError(int result, string functionName = null)
        {
            if (result != 0)
            {
                unsafe
                {
                    git_error error = git_error_last();

                    string errorMessage = U8StringConverter.ToManaged(error.message);

                    string mainMessage = functionName is null
                        ? $"libgit2 '{functionName}' returned non-zero value"
                        : "libgit2 returned non-zero value";

                    throw new InteropException(mainMessage, result, new Exception(errorMessage));
                }
            }
        }
    }
}
