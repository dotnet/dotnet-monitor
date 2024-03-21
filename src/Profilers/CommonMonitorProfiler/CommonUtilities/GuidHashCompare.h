#pragma once

    // hash compare class for using GUID in STL collections
    struct GuidHashCompare
    {
        enum
        {
            // Same as std::hash_compare
            bucket_size = 1
        };

        size_t operator()(const GUID& value) const
        {
            return value.Data1;
        }

        bool operator()(const GUID& key1, const GUID& key2) const
        {
            // test if _Keyval1 ordered before _Keyval2
            return memcmp(&key1, &key2, sizeof(GUID)) < 0;
        }
    };
