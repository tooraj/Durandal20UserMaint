﻿using System;
using System.Linq;
using System.Web.Http;
using System.Web.Security;
using Durandal2UserMaint.Models;
using WebMatrix.WebData;

namespace Durandal2UserMaint.Controllers
{
    [Authorize]
    public class AdditionalController : ApiController
    {
        public void ChangePassword(LocalPasswordModel model)
        {
            if (ModelState.IsValid)
            {
                bool valid = false;
                try
                {
                    valid = WebSecurity.ChangePassword(User.Identity.Name, model.OldPassword, model.NewPassword);    
                                        
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message);
                }
                if (!valid)
                {
                    throw new Exception("It appears your Old Password is NOT correct. Please try again.");
                }
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));

                //The linq expression above translates to the two foreach below
                //foreach (var modelState in ModelState.Values)
                //{
                //    foreach (var error in modelState.Errors)
                //    {
                //        errors += error.ErrorMessage + Environment.NewLine;
                //    }
                //}
                throw new Exception(errors);
            }
        }
        
        [HttpGet]
        public IQueryable<string> GetUserRoles()
        {
            return Roles.GetRolesForUser(WebSecurity.CurrentUserName).AsQueryable();
        }        

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IQueryable<UserGridModel> ListUsers()
        {
            var repo = new Durandal2UserMaintContext();

            return (from u in repo.UserProfiles
                    from m in repo.webpages_Membership
                    where u.UserId == m.UserId
                    select new UserGridModel
                    {
                        UserName = u.UserName,
                        CreateDate = m.CreateDate,
                        LastPasswordChange = m.PasswordChangedDate,
                        IsActive = u.IsActive,
                        Email = u.UserEmail
                    });
        }
        
        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IQueryable<RoleGridModel> ListRoles()
        {
            var repo = new Durandal2UserMaintContext();

            return (from v in repo.webpages_Roles select new RoleGridModel
            {
                RoleId = v.RoleId,
                RoleName = v.RoleName
            });
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IQueryable<UsersInRoleGridModel> ListUsersInRole(string role)
        {
            var repo = new Durandal2UserMaintContext();

            var q1 = (from v in repo.webpages_Roles where v.RoleName == role select v).FirstOrDefault();

            if (q1 != null)
            {
                return q1.UserProfiles.Select(userProfile => new UsersInRoleGridModel
                {
                    UserName = userProfile.UserName
                }).AsQueryable();
                
            }
            else
            {
                throw new Exception("Role not found");
            }
            
        }

        [HttpGet]
        [Authorize(Roles = "Administrator")]
        public IQueryable<RoleGridModel> ListUserRoles(string user)
        {
            var repo = new Durandal2UserMaintContext();

            var q1 = (from v in repo.UserProfiles where v.UserName == user select v).FirstOrDefault();

            if (q1 != null)
            {
                return q1.webpages_Roles.Select(x => new RoleGridModel
                {
                    RoleName = x.RoleName,
                    RoleId = x.RoleId
                }).AsQueryable();

            }
            else
            {
                throw new Exception("User not found");
            }

        }     
        
        //There is no reason to have the ActionName that I have in this class. I just added them in case it might help someone.
        [ActionName("UpdateUser")]
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void UpdateUserInfo(UserProfile userProfile)
        {
            if (ModelState.IsValid)
            {
                var repo = new Durandal2UserMaintContext();

                var user = (from v in repo.UserProfiles select v).FirstOrDefault(x => x.UserName == userProfile.UserName);
                if (user != null)
                {
                    user.UserEmail = userProfile.UserEmail;
                    user.IsActive = userProfile.IsActive;
                    repo.SaveChanges();
                }
                else
                {
                    throw new Exception("User to update not found");
                }
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }

        [ActionName("AddUser")]
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void AddUserInfo(AdminAddUser userProfile)
        {
            if (ModelState.IsValid)
            {
                //Yes, these try/catches I have through out don't do anything. You can either take them out in your code, or do something in the catch part.
                //Most likely you will be doing some kind of logging and beautifying the error before you send it back to the user.
                try
                {
                    //TODO: Check for the existence of the user ID already before trying to add it
                    WebSecurity.CreateUserAndAccount(userProfile.UserName, userProfile.Password, new { IsActive = userProfile.IsActive, UserEmail = userProfile.UserEmail });
                }
                catch (Exception)
                {                    
                    throw;
                }
                
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }

        [ActionName("ResetPassword")]
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void ResetUserPassword(ResetPassword userProfile)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var resetToken = WebSecurity.GeneratePasswordResetToken(userProfile.UserName, 1);
                    WebSecurity.ResetPassword(resetToken, userProfile.Password);
                }
                catch (Exception)
                {
                    throw;
                }

            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }
        
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void UpdateRole(webpages_Roles roleInput)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var repo = new Durandal2UserMaintContext();
                    var role =
                        (from v in repo.webpages_Roles where v.RoleId == roleInput.RoleId select v).FirstOrDefault();

                    if (role != null)
                    {
                        role.RoleName = roleInput.RoleName;
                        repo.SaveChanges();
                    }
                    else
                    {
                        throw new Exception("Role to update not found");
                    }
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }
        
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void AddRole(webpages_Roles roleInput)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //TODO: Check for the existence of the role already before trying to add it
                    Roles.CreateRole(roleInput.RoleName);                    
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }
        
        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void DeleteRole(webpages_Roles roleInput)
        {
            if (ModelState.IsValid)
            {
                try
                {                    
                    Roles.DeleteRole(roleInput.RoleName);
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void AddRoleToUser(UserRoleEditModel userRole)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    //I tried using the Roles.AddUserToRole but I was getting a foreign key error. So I decided this way.
                    
                    if (Roles.IsUserInRole(userRole.UserName, userRole.RoleName))
                    {
                        throw new Exception("User is already assigned that role");
                    }
                    var repo = new Durandal2UserMaintContext();
                    var user =
                        (from v in repo.UserProfiles select v).FirstOrDefault(x => x.UserName == userRole.UserName);
                    if (user == null)
                    {
                        throw new Exception("User to update not found");
                    }
                    else
                    {
                        var role =
                            (from v in repo.webpages_Roles select v).FirstOrDefault(x => x.RoleName == userRole.RoleName);
                        if (role == null)
                        {
                            throw new Exception("Role to use not found");    
                        }
                        else
                        {
                            user.webpages_Roles.Add(role);
                            repo.SaveChanges();    
                        }
                        
                    }

                }
                catch (Exception)
                {
                    
                    throw;
                }
                
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrator")]
        public void DeleteRoleFromUser(UserRoleEditModel userRole)
        {
            if (ModelState.IsValid)
            {
                Roles.RemoveUserFromRole(userRole.UserName, userRole.RoleName);
            }
            else
            {
                string errors = ModelState.Values.SelectMany(modelState => modelState.Errors).Aggregate(string.Empty, (current, error) => current + (error.ErrorMessage + Environment.NewLine));
                throw new Exception(errors);
            }
        }

    }
}
