# JwtIdentity.Api
simple boiler plate for jwt identity server using openiddict with seeding users

Most of this work is from:
https://github.com/openiddict/openiddict-core

Claims based policy reference:
http://benfoster.io/blog/asp-net-identity-role-claims


Integration Steps:
- open Startup.cs
- Add your user in line 158
- open Package Manager Console
- type: Update-Database
- run the application, should view swagger ui
- use the attached postman collection to hit api to get token
- in swagger where Authorization field is required enter "bearer [token from auth api endpoint]"
- if you remove profile.view from the administrator role, you will notice that your user is unable to authorize through authorized attributes with "View Profiles".
