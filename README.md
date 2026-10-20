RaceDay Event Management API (PROG6212 – Part 2)

System Description

RaceDay is a full-stack event management platform designed specifically for the South African road running, walking, and cycling community. This repository contains the backend RESTful API, which serves as the central backbone of the system. Built using ASP.NET Core and Entity Framework Core, the API manages all data operations, handles secure session-based authentication, enforces strict role-based access control, and provides the necessary endpoints for managing events, categories, participant enrolments, and race results.

User Roles

The system supports two distinct user roles, each with specific capabilities enforced at the API level. 

An Organiser has the ability to create, update, and delete their own events. They can define specific race categories (such as distance or age groups) for these events, view a complete list of all participants enrolled in their specific events, and capture participant results, including finish times and finishing positions. 

A Participant can create an account, browse through all available upcoming events, and enrol in an event by selecting their desired category. Participants can view their own enrolment history and track their personal performance through the "My Results" functionality, which displays their recorded finish times and positions.

Prerequisites

To run this application locally, the following tools must be installed on your machine: the .NET 8 SDK, SQL Server (LocalDB or Developer Edition), and Visual Studio 2022 (or Visual Studio Code).

Setup and Run Instructions

To set up the project, first clone the repository to your local machine using Git and navigate into the project root directory. Once inside, you must restore all necessary NuGet dependencies by running the `dotnet restore` command. 

Next, you need to create the database. Open the Package Manager Console in Visual Studio and execute the `Update-Database -Project RaceDay.Infrastructure -StartupProject RaceDay.API` command. This will apply the Entity Framework Core migrations and build the SQL Server schema.

Finally, to run the application, execute `dotnet run --project RaceDay.API`. Once the application is running, open your web browser and navigate to the Swagger UI at `https://localhost:<PORT>/swagger` to view and test the available endpoints.

Authentication and Security

The API utilises secure server-side session management rather than traditional JWT tokens. When a user logs in via the `/api/auth/login` endpoint, the API generates a unique session token and stores it in the database with an expiration time. 

For security, user passwords are never stored in plain text. They are cryptographically hashed using the PBKDF2 algorithm combined with a unique, randomly generated salt. 

To test protected endpoints within Swagger, you must first execute the login endpoint and copy the returned token. Click the green "Authorize" button at the top of the Swagger page and paste the token to authenticate your subsequent requests. If you are using external tools like Postman, you must attach the token to the request headers using the format: `Authorization: Bearer <YOUR_TOKEN>`.

Testing

The application includes a comprehensive suite of automated unit tests developed using xUnit. These tests are designed to verify the success and failure scenarios of user registration, login, role-based access enforcement, and event enrolment logic. The tests utilise an in-memory database to ensure they run quickly and do not interfere with your actual SQL Server data. You can run the tests locally using the `dotnet test` command.

Continuous Integration and Deployment (CI/CD)

This repository is configured with GitHub Actions for automated Continuous Integration. Every time code is pushed to the repository, a workflow automatically triggers to restore dependencies, build the project in Release mode, and execute all unit tests to ensure no breaking changes have been introduced.


Part 1 Artefacts

All planning documents from Part 1, including the Entity Relationship Diagram (ERD), the API endpoint plan, and the SQL database script, have been preserved and are located inside the `/docs` folder in the root of this repository.

Video Presentation

A comprehensive video walkthrough of this project is available. The video demonstrates the running application, explains the code structure, showcases the authentication and role-based logic, and demonstrates the unit tests passing.

YouTube Video Link: 
