using Microsoft.EntityFrameworkCore;

namespace Blog.Services.Identity.API.Models;

public class IdentityDbContext<TUser> : DbContext where TUser : UserBase
{
    public DbSet<TUser> Users { get; set; }

    public IdentityDbContext(DbContextOptions<IdentityDbContext<TUser>> options) : base(options)
    {

    }

}