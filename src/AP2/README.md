# Experimental: Dataverse AI Assistant (Copilot)

This is an experimental project to explore the use of Azure AI services with Dataverse. 
The project is based on the [Dataverse Web API](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/webapi/overview) and 
the [Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) with use of Azure AI Functions.

## Demos
[Watch Short Demos](docs/Demos.md)

## Contributing
This project is welcoming contributions. If you have any questions, feel free to start a [discussion](https://github.com/petrochuk/DataverseSamples/discussions).

## Dataverse AI Assistant Skills/Functons

The following skills are implemented as Azure AI Functions in [DataverseAIClientSkills.cs](DataverseAzureAI/DataverseAIClientSkills.cs):

| Skill | Description |
| ----- | ----------- |
| FindTableByName | Find Dataverse table or entity by exact or partial name |
| ListOfTablesByPropertyValue | Returns filtered list of all tables based on specified property value |
| GetTablePropertyValue | Returns property value for specified table |

## Running the project

Open appSettings.json and update the following settings:

| Section | Setting | Description |
| ------- | ------- | ----------- |
| TestApp | EnvironmentId | Dataverse environment id |
| AzureAI | OpenApiEndPoint | Azure AI Open API endpoint |
| AzureAI | OpenApiKey | Azure AI Open API key |
| AzureAI | OpenApiModel | Azure AI Open API model which supports Azure AI Functions version **0613** of gpt-35-turbo, gpt-35-turbo-16k, gpt-4, and gpt-4-32k |
