using Fraga.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fraga.Infrastructure.Data.Configurations;

/**
 * Configura o mapeamento da entidade User
 * para a tabela de usuários no PostgreSQL.
 */
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /**
     * Define propriedades, índices e relacionamento
     * entre User e Account.
     */
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(255)
            .IsRequired();

        /**
         * Garante que dois usuários não possam
         * utilizar o mesmo e-mail.
         */
        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.Property(user => user.PasswordHash)
            .IsRequired();

        builder.Property(user => user.AccountId)
            .IsRequired();

        /**
         * Cada usuário possui uma única conta
         * financeira associada.
         */
        builder.HasOne(user => user.Account)
            .WithOne(account => account.User)
            .HasForeignKey<User>(user => user.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}