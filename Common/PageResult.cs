namespace DeviceManagementOnly.Common
{
    public class PaginationParams
    {
        private const int MaxPageSize = 100;
        private int? _pageSize;

        public int? Page { get; set; }

        public int? PageSize
        {
            get => _pageSize;
            set => _pageSize = value.HasValue
                ? (value.Value > MaxPageSize ? MaxPageSize : value.Value < 1 ? 1 : value.Value)
                : null;
        }

        public string? Search { get; set; }

        public bool IsPaginated => Page.HasValue && PageSize.HasValue;
    }

    public class PagedResult<T>
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int TotalRecords { get; set; }

        public int TotalPages => (Page.HasValue && PageSize.HasValue && PageSize.Value > 0)
            ? (int)Math.Ceiling((double)TotalRecords / PageSize.Value)
            : 1;

        public bool HasPreviousPage => Page.HasValue && Page > 1;
        public bool HasNextPage => Page.HasValue && Page < TotalPages;
        public bool IsPaginated => Page.HasValue && PageSize.HasValue;

        public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();

        public static PagedResult<T> Create(IEnumerable<T> data, int totalRecords, PaginationParams p) => new()
        {
            Page = p.IsPaginated ? p.Page : null,
            PageSize = p.IsPaginated ? p.PageSize : null,
            TotalRecords = totalRecords,
            Data = data
        };
    }

    public class DeviceMasterFilterParams : PaginationParams
    {
        public Guid? DeviceCategoryId { get; set; }
        public Guid? DeviceTypeId { get; set; }
        public Guid? CompanyId { get; set; }
    }

    public class ModelSpecificationFilterParams : PaginationParams
    {
        public Guid? DeviceTypeId { get; set; }
        public Guid? CompanyId { get; set; }
    }

    public class DeviceDetailFilterParams : PaginationParams
    {
        public Guid? DeviceMasterId { get; set; }
        public Guid? CompanyId { get; set; }
        public Guid? DeviceCategoryId { get; set; }
        public Guid? DeviceTypeId { get; set; }
    }

    public class RoomFilterParams : PaginationParams
    {
        public Guid? FloorId { get; set; }
    }

    public class RackFilterParams : PaginationParams
    {
        public Guid? RoomId { get; set; }
        public Guid? FloorId { get; set; }
        public string? Status { get; set; }
    }

    public class RackPowerCapacityFilterParams : PaginationParams
    {
        public Guid? RackId { get; set; }
    }

    public class DeviceRackLocationFilterParams : PaginationParams
    {
        public Guid? DeviceDetailId { get; set; }
        public Guid? RackId { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
    }

    public class DeviceConnectionConfigFilterParams : PaginationParams
    {
        public Guid? DeviceDetailId { get; set; }
        public string? Protocol { get; set; }
        public bool? IsActive { get; set; }
    }

    public class NetworkConnectionFilterParams : PaginationParams
    {
        public Guid? SourceDeviceDetailId { get; set; }
        public Guid? TargetDeviceDetailId { get; set; }
        public Guid? DeviceDetailId { get; set; }   // dono side check karega (source ya target)
        public string? Status { get; set; }
        public string? CableType { get; set; }
    }

    public static class PaginationExtensions
    {
        public static async Task<PagedResult<T>> ToPagedAsync<T>(
            this IQueryable<T> query,
            PaginationParams p,
            CancellationToken cancellationToken = default)
        {
            if (p.IsPaginated)
            {
                var totalRecords = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .CountAsync(query, cancellationToken);

                var data = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .ToListAsync(
                        query.Skip((p.Page!.Value - 1) * p.PageSize!.Value).Take(p.PageSize.Value),
                        cancellationToken);

                return PagedResult<T>.Create(data, totalRecords, p);
            }
            else
            {
                var data = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                    .ToListAsync(query, cancellationToken);

                return PagedResult<T>.Create(data, data.Count, p);
            }
        }
    }
}
