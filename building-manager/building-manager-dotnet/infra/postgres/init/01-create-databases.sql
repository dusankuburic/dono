-- Creates the databases the application connection strings expect.
-- buildingmanager (POSTGRES_DB) holds the EF Core relational data,
-- building_manager_orleans holds the Orleans clustering/persistence tables.

CREATE DATABASE building_manager;
CREATE DATABASE building_manager_orleans;
