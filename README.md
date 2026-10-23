# GDELT-Lab Prototype

This repository contains two runnable prototypes developed to explore GDELT data and experiment with different approaches to presenting news information.

**This is a research and prototyping repository for Project 300. It is not the final Project 300 application.**

## 1. The Two Prototypes

### 1.1 GDELT-Lab

GDELT-Lab is primarily a data inspection and exploration tool for working with GDELT datasets.

It was developed to map and investigate the available fields within GDELT data and display the underlying raw JSON records. This makes it easier to understand the structure of the data, examine individual records, and investigate which fields and information may be useful for a future news intelligence platform.

The prototype focuses on three GDELT datasets:

- **GKG (Global Knowledge Graph):** Information extracted from news coverage, including themes, entities, people, organisations, and locations.
- **Events:** Structured records representing reported events.
- **Mentions:** Records describing mentions of events in news sources.

The GDELT-Lab frontend and backend are separate applications that work together to provide the data exploration functionality.

### 1.2 News Feed and Article Details Prototype

The News Feed prototype explores different ways of presenting news articles and their details to users.

It consists of an Angular frontend and a .NET API backend, with article data stored in a separate SQLite database.

The frontend contains five experimental designs:

- **Design A:** Baseline news feed.
- **Design B:** Dark, editorial-style layout.
- **Design C:** Traditional newsroom layout.
- **Design D:** Intelligence dashboard layout.
- **Design E:** Swiss-inspired minimalist layout.

The purpose of these designs is to compare different layouts and approaches to displaying news articles and their associated information.

## 2. Repository Structure

```text
GDELT-Lab/
├── backend/
│   ├── GDELT-Lab.csproj
│   ├── NewsFeed.Api/
│   └── NewsFeed.DataPrep/
├── frontend/
│   ├── GDELT-Lab/
│   └── NewsFeed.Client/
├── research-data/
│   ├── news-feed/
│   └── reference-data/
├── data/
│   └── gdelt-lab.db
├── scripts/
└── README.md
```

### Supporting projects

- **NewsFeed.Api:** The .NET backend API for the News Feed prototype.
- **NewsFeed.Client:** The Angular frontend for the News Feed and Article Details prototype.
- **NewsFeed.DataPrep:** A supporting project for preparing or processing data for the News Feed prototype.
- **ArticleMetadata:** A project for article metadata processing and experimentation.
- **ArticleMetadataTest:** The associated test project for article metadata functionality.

## 3. Database and Research Data Setup

### 3.1 Obtain the GDELT-Lab database

The main GDELT-Lab database is too large to store directly in this GitHub repository.

1. Open the group's shared OneDrive folder.
2. Download the compressed file named **`data.zip`**.
3. Extract `data.zip`.
4. Locate the extracted `data` folder containing `gdelt-lab.db`.
5. Copy the entire `data` folder into the root directory of your cloned `GDELT-Lab` repository.

The resulting path should be:

```text
GDELT-Lab/
├── data/
│   └── gdelt-lab.db
├── backend/
├── frontend/
└── README.md
```

**Important:** The `data` folder must be directly inside the repository root, not inside `backend`, `frontend`, or another folder. The expected database path is `data/gdelt-lab.db`.

Do not rename the database file.

### 3.2 News Feed database

The News Feed prototype uses a separate SQLite database:

```text
research-data/news-feed/news-feed.db
```

This database is distinct from `data/gdelt-lab.db`. The two databases support different prototypes and should remain at their respective paths.

### 3.3 GDELT theme reference data

The repository uses GDELT theme reference data to help translate raw theme codes into readable labels.

Reference files are stored under:

```text
research-data/reference-data/
```

The compact dictionary used by the Angular News Feed client is located at:

```text
frontend/NewsFeed.Client/public/data/
```

The research reference files and the frontend dictionary serve different purposes. The reference files support research and dictionary generation, while the compact dictionary is loaded by the frontend to display readable theme labels.

### 3.4 Extracted GDELT datasets

The complete `research-data/extracted/` directory can occupy considerable disk space. It is not required simply to run the existing prototypes if the required databases and reference files are available.

Download or extract the larger research datasets only when you need them for additional GDELT experiments or data preparation.

## 4. Running the Prototypes

Run commands from the repository root unless otherwise stated.

### 4.1 Run the News Feed frontend

Open a terminal in the repository root and run:

```powershell
cd frontend/NewsFeed.Client
ng serve --o
```

### 4.2 Run the News Feed backend

Open a separate terminal in the repository root and run:

```powershell
dotnet run --project .\backend\NewsFeed.Api\NewsFeed.Api.csproj
```

Keep the backend running while using the News Feed frontend.

### 4.3 Run the GDELT-Lab backend

Open a terminal in the repository root and run:

```powershell
dotnet run --project backend/GDELT-Lab.csproj --launch-profile http
```

### 4.4 Run the GDELT-Lab frontend

Open a separate terminal in the repository root and run:

```powershell
cd frontend/GDELT-Lab
ng serve --o
```

The two prototypes have separate frontends and backends. Run the matching pair for whichever prototype you want to use.

## 5. Inspecting SQLite Databases

You can inspect the databases using **DB Browser for SQLite**, a graphical application for opening, querying, and examining SQLite database files.

### Download and install

1. Visit the official download page: https://sqlitebrowser.org/dl/
2. Download the appropriate installer for your operating system. For a typical Windows computer, choose the Windows installer.
3. Run the installer and follow the installation instructions.
4. Open DB Browser for SQLite.

### Open a database

1. Select **Open Database**.
2. Navigate to the database you want to inspect, such as:

   ```text
   data/gdelt-lab.db
   ```

   or:

   ```text
   research-data/news-feed/news-feed.db
   ```

3. Select and open the database file.
4. Use **Browse Data** to inspect tables and records.
5. Use **Execute SQL** to run SQL queries against the selected database.

**Important:** Make sure you open the correct database. The GDELT-Lab and News Feed prototypes use separate files.

When experimenting with queries, avoid modifying or deleting records unless you intend to change your local database.

## 6. Research Data Setup Script

A PowerShell setup script is available at:

```text
scripts/setup-research-data.ps1
```

After downloading and placing the required data files in their expected locations, you can run the script from the repository root:

```powershell
.\scripts\setup-research-data.ps1
```

The script is intended to help check the local research-data setup. It complements the database download and placement instructions above.

## 7. Troubleshooting

### GDELT-Lab cannot find its database

- Check that `data/gdelt-lab.db` exists.
- Confirm that the `data` folder is directly inside the repository root.
- Make sure the database was fully extracted from `data.zip`.

### The News Feed does not display articles

- Confirm that `research-data/news-feed/news-feed.db` exists.
- Make sure the NewsFeed.Api backend is running.
- Check the backend terminal for database or API errors.
- Check the browser developer console for frontend errors.

### GDELT theme codes appear instead of readable labels

- Confirm that the compact theme-label dictionary exists under `frontend/NewsFeed.Client/public/data/`.
- Check that the frontend can load the dictionary successfully.
- Check the browser console for errors loading the dictionary.

### A frontend does not start

- Confirm that you are in the correct frontend directory.
- Run `ng serve --o` from the appropriate directory.
- Check the terminal output for dependency or compilation errors.

### A backend does not start

- Confirm that you are running the correct `.csproj` file.
- Check the terminal output for database, configuration, or port errors.
- Ensure that the required local data files are present.
