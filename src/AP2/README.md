# Experimental: Dataverse AI Assistant (Copilot)

This is an experimental project to explore the use of Azure AI services with Microsoft Power Platform and Dataverse. 
The project is based on the [Dataverse Web API](https://docs.microsoft.com/en-us/powerapps/developer/data-platform/webapi/overview) and 
the [Azure Cognitive Services](https://azure.microsoft.com/en-us/services/cognitive-services/) with use of Azure AI Functions.

It can be used as a starting point for building your own AI assistant for Microsoft Power Platform. ConsoleTestApp is
a sample console application that demonstrates how to use it. You can add more skills to DataverseAIClient class or derive
from it to create your own client. During runtime, the client will automatically discover all skills and their parameters.

## Demos

[Watch Short Demos](docs/Demos.md)

## Contributing

This project is welcoming contributions. If you have any questions, feel free to start a [discussion](https://github.com/petrochuk/DataverseSamples/discussions).

## Dataverse AI Assistant Skills/Functons

The following skills are implemented as Azure AI Functions in [DataverseAIClientSkills.cs](DataverseAzureAI/DataverseAIClientSkills.cs):

| Skill | Description |
| ----- | ----------- |
| FindTableByName | Find Dataverse table or entity by exact or partial name |
| ListOfTablesByPropertyValue | Returns filtered list of tables based on specified property value |
| GetTablePropertyValue | Returns property value for specified table |
| ListOfModelDrivenAppsByPropertyValue | Returns filtered list of Model-driven PowerApps based on specified property value |
| ListOfCanvasAppsByPropertyValue | Returns filtered list of canvas PowerApps based on specified property value |
| GetCanvasAppPropertyValue | Returns property value for a canvas app |
| ListOSolutionsByPropertyValue | Returns filtered list of Dataverse solutions based on specified property value and optional user |
| GetSolutionPropertyValue | Returns property value for specified Dataverse solution |

## Running the project

Open appSettings.json and update the following settings:

| Section | Setting | Description |
| ------- | ------- | ----------- |
| TestApp | EnvironmentId | Dataverse environment id |
| AzureAI | OpenApiEndPoint | Azure AI Open API endpoint |
| AzureAI | OpenApiKey | Azure AI Open API key |
| AzureAI | OpenApiModel | Azure AI Open API model which supports Azure AI Functions version **0613** of gpt-35-turbo, gpt-35-turbo-16k, gpt-4, and gpt-4-32k |
