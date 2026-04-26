using AMS.Domains.Data;
using AMS.Domains.Dto;
using AMS.Domains.Entities;
using Microsoft.EntityFrameworkCore;

namespace AMS.Services.DBService;

public class UserAccountService
{
    private readonly IDbContextFactory<DataContext> contextFactory;

    public UserAccountService(IDbContextFactory<DataContext> contextFactory)
    {
        this.contextFactory = contextFactory;
    }

    public async Task<List<UserAccount>> GetUserAccounts()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.UserAccounts.ToListAsync();
    }

    public async Task<UserAccount> GetUserAccount(Guid id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.UserAccounts.FindAsync(id);
    }

    public async Task<UserAccount> AddUserAccount(RegisterDto register)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        register.FullName = (register.FullName ?? "").Trim();
        register.Username = (register.Username ?? "").Trim();
        register.Password = (register.Password ?? "").Trim();

        if (string.IsNullOrWhiteSpace(register.FullName) ||
            string.IsNullOrWhiteSpace(register.Username) ||
            string.IsNullOrWhiteSpace(register.Password))
        {
            throw new InvalidOperationException("Please fill in all required fields.");
        }

        var usernameExists = await context.UserAccounts.AnyAsync(x => x.Username == register.Username);
        if (usernameExists)
        {
            throw new InvalidOperationException("Username already exists. Please choose another one.");
        }

        UserAccount userAccount = new UserAccount
        {
            Oid = Guid.NewGuid(),
            FullName = register.FullName,
            Username = register.Username,
            Password = register.Password,
            UserType = register.UserType
        };
        context.UserAccounts.Add(userAccount);
        await context.SaveChangesAsync();
        
        return userAccount;
    }
    
    public async Task<UserAccount> UpdateUserAccount(Guid key,RegisterDto register)
    {
       await using var context = await contextFactory.CreateDbContextAsync();
       var userAccount = await context.UserAccounts.FindAsync(key);
       
       userAccount.FullName = register.FullName;
       userAccount.Username = register.Username;
       userAccount.Password = register.Password;
       userAccount.UserType = register.UserType;
       
        context.UserAccounts.Update(userAccount);
        await context.SaveChangesAsync();
        
        return userAccount;
    }
    
    public async Task<UserAccount> DeleteUserAccount(Guid key)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var userAccount = await context.UserAccounts.FindAsync(key);
       
        context.UserAccounts.Remove(userAccount);
        await context.SaveChangesAsync();
        
        return userAccount;
    }

    public async Task<UserAccount> Login(LoginDto login)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var userAccount = await context.UserAccounts.FirstOrDefaultAsync(x => x.Username == login.Username && x.Password == login.Password);

        return userAccount;
    }
}
