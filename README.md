# DataverseSamples

## Dataverse Copilot Demo with PAC CLI

This sample demonstrates how to create simple Dataverse Copilot with Power Apps CLI to interact with data stored in Dataverse without need 
to modify your Dataverse environment or send any of your proprietary data to OpenAI. 
It uses ChatGPT-35 turbo model to generate FetchXML queries and any regular Dataverse environment.

Endpoints can be configured in the `appSettings.json` file.

### Interact with data in Dataverse 

![Dataverse Copilot Demo with PAC CLI](media/all-accounts-search.gif)

### Using custom tables with joins and filters

![Custom Table](media/custom_table.gif)

### Functional flow block diagram

1. User types query in natural language for example: "show me all accounts"	
2. The example app adds system prompt to the query: "return only FetchXML queries" to instruct AI model to return only FetchXML queries only
3. AL model resonds with FetchXML query but it cannot be executed because metadata doesn't match current Dataverse environment
4. The app modifies the FetchXML query to fix tables, attributes, links etc metadata by quering Dataverse metadata API
5. After query matches current Dataverse environment the app executes the query and displays results

![Custom Table](media/FunctionalFlow.png)

