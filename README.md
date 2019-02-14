# dbdeploy.net

##TransCanada fork

This fork contains in-house customizations that were useful for a couple of active projects in the organization.

### Feature: Support gaps in database script version numbers

On a team with many members who are either working on the same or different releases of an application,
we found that allowing gaps in numbering was more flexible.

Imagine we are working on a server-side application which had a 1.0 release three months ago.
The 1.0 release has change scripts 1 to 39 already applied.

The team is also working on new 2.0 features expected to go live next month.
As new 2.0 features are completed, application and database changes are deployed to test servers.
So far, 2.0 features have created scripts 40, 41, 42.

An urgent problem in the 1.0 release was found, requiring a new change script.
Calling this script 43 would not work because the 2.0 release has not gone live yet.
Calling this script 40 would work, but then the 2.0 scripts need to be renumbered,
which is problematic because our test server already had the previous numbers applied.

Allowing gaps in numbering helps everyone keep working.
The urgent problem should be script 43.
After applying it, our production database contains changes 1-39, 43.
The 2.0 release would contains scripts 40-42, 44-53.
Source control history on the change scripts tells us exactly which release any particular script was intended for.

By the way, how does a team member know that the next number to use is 43?
We use a lightweight approach on our team wiki.
The convention is that any time a team member intends to create a change script, they go to a central wiki page which might look like:

* 42 - (v2.0) add effective dates to contract table
* 41 - (v2.0) amalgamate market 5 into 4
* 40 - (v2.0) remove IsConfirmed flag from parameter table
* 39 - (v1.0) correct security capability records

And add a new line to the top of that list:

* 43 - (v1.0) increase penalty rate precision

### Feature: Console exits with code 1 to indicate problems

In automated scripted deployments, if this tool encounters a problem (eg. invalid credentials accessing the database for applied changes),
it did not exit with a different code, so the calling script thinks that everything is good and continues.
We made the tool exit with code 1 which would signal the calling script to stop.

### Feature: Censor connection strings with passwords in log entries

In our automated scripted deployments, a separate SQL Server database account is used to read the database's changelog and apply required changes.
The account credentials are embedded into the connection string by the deployment system.
Dbdeploy logs this connection string to the console by default, which is being captured by the deployment system.
We did not want this, so this feature censors the connection string when it detects "password" in it.

## Original Content from rakker91/dbdeploy.net

A NuGet package is now available at http://nuget.org/packages/DbDeployNet2/

For usage instructions, please see the /docs folder.  There's a help file and some more detailed instructions on usage.

One of the major challenges that many products face is around versioning of databases between development and production. DbDeploy was developed to meet this need (see http://code.google.com/p/dbdeploy/). 

In 2007, DbDeploy.Net was ported from the DbDeploy version 2.0. (see http://sourceforge.net/projects/dbdeploy-net/). The project appears to have seen little changes since then.

Recently, we wanted to use DbDeploy against Microsoft's Sql Azure platform and discovered that it simply doesn't work.

We investigated what it'd take to update DbDeploy.Net to a version that would support it and because of the code base, we decided that rewriting would be a better strategy.

This version is a complete rewrite from the ground up. While many of the concepts and ideas are similar, we've added the following:
Full Unit Testing with NUnit 
Support for Azure (and easy maintenance of the Sql used in the application 
Similar functionality to version 3.0 of DbDeploy (elimination of ChangeSets, simplification of the changelog table) 
Stylecop Compliance with the default StyleCop rules 
Dependency injection and service architecture 
Better MS Build integration and easier command line integration 
Sandcastle Help Documentation

We plan on keeping this project active and making changes as needed based on suggestions made by you. Some things that we plan on adding in the near future:
Support for database servers other than MsSql

Currently, this version is compatible with scripts made for other version of DbDeploy, but only Microsoft Sql Server is supported at this time! If you'd like to help us out with other DBMS', please drop us a line.

We appreciate your feedback.
