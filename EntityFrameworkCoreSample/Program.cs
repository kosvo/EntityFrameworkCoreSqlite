using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;

namespace EntityFrameworkCoreSample
{
    class Program
    {
        static void Main(string[] args)
        {
            string dbName = "TestDatabase.db";
            if (File.Exists(dbName))
            {
                File.Delete(dbName);
            }
            using (var dbContext = new MyDbContext())
            {
                var stopwatch = new Stopwatch();
                //Ensure database is created
                dbContext.Database.EnsureCreated();
                if (!dbContext.Blogs.Any())
                {
                    stopwatch.Start();
                    Console.WriteLine($"Start={stopwatch.Elapsed}");
                    for (var i = 1; i <= 100000; i++)
                    {
                        dbContext.Blogs.Add(new Blog
                        {
                            BlogId = i,
                            Title = "Blog " + i,
                            SubTitle = $"Blog {i} subtitle",
                            Authors = new List<Autor> { new Autor { AutorId = i, Name = "autor " + i, Surname = "Surname " + i } }
                        });
                    }
                    dbContext.SaveChanges();
                    Console.WriteLine($"Finish save={stopwatch.Elapsed}");


                    Console.WriteLine($"Start update={stopwatch.Elapsed}");
                    dbContext.Blogs.ForEachAsync(a => a.Title = a.Title + " 2").Wait();

                    dbContext.SaveChanges();
                    Console.WriteLine($"Finish Saved={stopwatch.Elapsed}");


                    //using (var transaction = dbContext.Database.BeginTransaction())
                    //{
                    Console.WriteLine($"Start BulkInsert={stopwatch.Elapsed}");
                    var list = new List<Blog>();
                    for (var i = 100001; i <= 200000; i++)
                    {
                        list.Add(new Blog
                        {
                            BlogId = i,
                            Title = "Blog " + i,
                            SubTitle = $"Blog {i} subtitle",
                            Authors = new List<Autor> { new Autor { AutorId = i, Name = "autor " + i, Surname = "Surname " + i } }
                        });
                    }
                    var bulkConfig = new BulkConfig { PreserveInsertOrder = true, SetOutputIdentity = true };
                    dbContext.BulkInsert<Blog>(list, bulkConfig);

                    dbContext.SaveChanges();
                    Console.WriteLine($"Finish BulkInsert save blogs={stopwatch.Elapsed}");
                    //   }
                }
                // foreach (var blog in dbContext.Blogs)
                // {
                Console.WriteLine($"Count={dbContext.Blogs.Count()}\tTime={stopwatch.Elapsed}");
                // }
            }
            Console.ReadLine();
        }
    }
    /// <summary>
    /// Blog entity
    /// </summary>
    public class Blog
    {
        [Key]
        public int BlogId { get; set; }
        [Required]
        [MaxLength(128)]
        public string Title { get; set; }
        [Required]
        [MaxLength(256)]
        public string SubTitle { get; set; }
        [Required]
        public DateTime DateTimeAdd { get; set; }

        public List<Autor> Authors { get; set; } = new List<Autor>();
    }

    public class Autor
    {
        [Key]
        public int AutorId { get; set; }
        [Required]
        [MaxLength(128)]
        public string Name { get; set; }
        [Required]
        [MaxLength(256)]
        public string Surname { get; set; }
        [Required]
        public DateTime DateTimeAdd { get; set; }

        [Required]
        public Blog Blog { get; set; }

    }

    public class MyDbContext : DbContext
    {
        public DbSet<Blog> Blogs { get; set; }
        public DbSet<Autor> Autors { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Filename=TestDatabase.db", options =>
            {
                options.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName);
            });
            base.OnConfiguring(optionsBuilder);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Map table names
            modelBuilder.Entity<Blog>().ToTable("Blogs", "test");

            modelBuilder.Entity<Blog>(entity =>
            {
                entity.HasKey(e => e.BlogId);
                entity.HasIndex(e => e.Title).IsUnique();
                entity.HasMany(c => c.Authors).WithOne(b => b.Blog);
                entity.Property(e => e.DateTimeAdd).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Autor>().ToTable("Autors", "test");
            modelBuilder.Entity<Autor>(entity =>
            {
                entity.HasKey(e => e.AutorId);

                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.DateTimeAdd).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}
