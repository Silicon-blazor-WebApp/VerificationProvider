
using Microsoft.EntityFrameworkCore;
using VerificationProvider.Data.Entites;

namespace VerificationProvider.Data.Context;

public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
{
    public DbSet<VerificationRequestEntity> VerificationRequests { get; set; }
}
