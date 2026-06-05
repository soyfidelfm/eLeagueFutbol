namespace backend.Entities.Common;

public abstract class BaseEntity
{
	public DateTime CreatedAtUTC { get; set; }

	public DateTime? UpdatedAtUTC { get; set; }

	public bool IsDeleted { get; set; }

	public Guid? CreatedByUserId { get; set; }

	public Guid? UpdatedByUserId { get; set; }
}