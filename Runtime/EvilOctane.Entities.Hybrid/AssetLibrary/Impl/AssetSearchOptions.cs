using System;

namespace EvilOctane.Entities
{
    [Flags]
    public enum AssetSearchOptions
    {
        None,
        LogErrors = 1 << 0,
        UseFirstIfMultipleExist = 1 << 1,
        OptionalIfNameIsEmpty = 1 << 2,

        RequiredStrict = LogErrors,
        OptionalStrict = LogErrors | OptionalIfNameIsEmpty
    }
}
