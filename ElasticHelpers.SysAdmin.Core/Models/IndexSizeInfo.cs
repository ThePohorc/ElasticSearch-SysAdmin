namespace ElasticHelpers.SysAdmin.Core.Models;

public record IndexSizeInfo(
    string Health,
    string Status,
    string Index,
    string DocsCount,
    string StoreSize
);
