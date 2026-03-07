using InstruaMe.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InstruaMe.Infrastructure.Mappings
{
    public class ChatMessageMap : IEntityTypeConfiguration<ChatMessage>
    {
        public void Configure(EntityTypeBuilder<ChatMessage> builder)
        {
            builder.ToTable(nameof(ChatMessage));

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Content).HasMaxLength(4000);
            builder.Property(x => x.SenderRole).HasMaxLength(20);

            builder.HasOne(x => x.Conversation)
                .WithMany(x => x.Messages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
