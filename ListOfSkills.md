# Experimental: Copilot/AI Assistant for Microsoft Power Platform

> [!WARNING]
> **This repository is still work-in-progress and expected to undergo significant changes**

## Copilot/AI Assistant Skills implemented as AI Functons

The following skills are implemented as Azure AI Functions in [DataverseAIClientSkills.cs](src/DataverseAzureAI/DataverseAIClientSkills.cs):

| Skill | Description |
| ----- | ----------- |
| GetTableDescription | Returns description for Dataverse table or entity |
| ListOfTablesByPropertyValue | Returns filtered list of tables based on specified property value |
| GetTablePropertyValue | Returns property value for specified table |
| ListOfModelDrivenAppsByPropertyValue | Returns filtered list of Model-driven PowerApps based on specified property value |
| FindCanvasApp | Finds a canvas app based on specified property value |
| ListOfCanvasAppsByPropertyValue | Returns all or filtered list of canvas apps based on specified property value |
| GetCanvasAppPropertyValue | Returns property value for a canvas app |
| NaturalLanguageToSqlQuery | Takes in query text as a parameter and converts it to SQL query and executes the SQL query on Dataverse and sends the result back. It knows about systemuser, teams and roles. It can convert any natural language query to SQL query and return result of that SQL query |
| ListOSolutionsByPropertyValue | Returns all solutions or filtered list of installed/imported based on specified property value and optional user |
| ListOSolutionComponents | Returns list of components inside Dataverse solutions. It can filter on component type, name or other properties |
| AddSolutionComponent | Adds component to Dataverse solution |
| GetSolutionPropertyValue | Returns property value for specified Dataverse solution |
| ExportSolution | Exports Power Platform solution and saves it to local file |
| SendEmailOrShareLinkWithSomeone | Sends an email or shares a link to an item, record or anything else inside PowerPlatform including but not limited to app, solution, table, component |
| SaveToFileSystem | Save to file system: text output, apps, solutions, lists or any other Power Platform component |
| CreateItemInsidePowerPlatform | Creates new objects inside Power Platform including but not limited to apps, solutions, tables, users, components, records |
| ListOfRoles | List of roles |
| ListOfRoleMembers | List of role members |
| ListOfUserRoles | Returns list of roles assigned to a user |
| CheckRolePrivilege | Check if role has specific privilege |
| UpdateUserPermission | Updates user roles inside Power Platform |
| ShareCanvasApp | Share canvas app by giving permission |
| ListActiveDirectorySecurityGroups | Returns list of Azure Active Directory security groups person member of by calling Microsoft Graph API |

