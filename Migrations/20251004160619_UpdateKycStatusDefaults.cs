using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace stibe.api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateKycStatusDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing users who have 'Pending' status but no KYC submission date
            // This indicates they were assigned 'Pending' by default, not through actual submission
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET KycStatus = 'NotStarted' 
                WHERE KycStatus = 'Pending' 
                AND KycSubmittedAt IS NULL
                AND IsKycVerified = 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert the changes if needed
            migrationBuilder.Sql(@"
                UPDATE Users 
                SET KycStatus = 'Pending' 
                WHERE KycStatus = 'NotStarted'
            ");
        }
    }
}
