# JwtIdentity.Api
simple boiler plate for jwt identity server using openiddict with seeding users

Most of this work is from:
https://github.com/openiddict/openiddict-core


Integration Steps:
- open Startup.cs
- Add your user in line 158
- open Package Manager Console
- type: Update-Database
- run the application, should view swagger ui
- use the attached postman collection to hit api to get token
- in swagger where Authorization field is required enter "bearer [token from auth api endpoint]"
