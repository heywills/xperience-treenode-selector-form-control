using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XperienceCommunity.TreeNodeSelectorFormControl.Constants
{
    public static class QueryConstants
    {
		// The QueryName and ClassName are required for backward compatibility.
        public const string QueryName = "relatedcontentselectortree";
		public const string ClassName = "cms.root";
        public const string FullyQualifiedQueryName = ClassName + "." + QueryName;

        public const string QueryText = 
@"/*  
    Query required by ~\CMSFormControls\BlueModus\RelatedContentSelector\TreeSelectorDialog\TreeSelectorDialog.aspx.cs
	This query is provided by the Nuget package XperienceCommunity.TreeNodeSelectorFormControl.
	Changes to this query will be lost when this package is restored or updated.   
*/

-- TEST PARAMETERS START
/*
DECLARE @SiteID int;
DECLARE @Culture nvarchar(50);
DECLARE @SelectedNodeGuids [Type_CMS_GuidTable];
DECLARE @SelectablePageTypes [Type_CMS_StringTable];
DECLARE @StartingNodeAliasPath nvarchar(450);
DECLARE @SearchText nvarchar(50);

SET @SiteID=1;
SET @Culture=N'en-US';
SET @StartingNodeAliasPath='/Content-Library';
SET @SearchText='';

insert into @SelectedNodeGuids values('41724579-5430-417F-BB3B-EBC40395D565');
insert into @SelectedNodeGuids values('36BA30DF-9710-41F8-A674-E120374051ED');
insert into @SelectedNodeGuids values('6C94183E-33DB-4AFE-BEE5-C24F531ADCF9');
insert into @SelectedNodeGuids values('B270C5E6-30B1-41E1-A4B8-0CDBBC0B94D7');

insert into @SelectablePageTypes values(N'ContentBase.AccordionItemSimple')
insert into @SelectablePageTypes values(N'ContentBase.FAQItem');

*/

-- TEST PARAMETERS END
/*
NOTE: Before adding the SearchText condition we used one easier
to maintain SQL statement, without concatenation and sp_executesql.
We'd just negate logic conditions for blank parameters.
However, the performance was degrated significantly with the SearchText
parameter, even when it was negated.
*/

DECLARE @sql nvarchar(max)
SET @sql =
N'
DECLARE @SearchLikeExpression nvarchar(52);
SET @SearchLikeExpression=N''%'' + @SearchText + N''%'';
DECLARE @StartingPathLikeExpression nvarchar(452);
SET @StartingPathLikeExpression = 
CASE
	WHEN @StartingNodeAliasPath = '''' OR @StartingNodeAliasPath IS NULL THEN ''/%''
	WHEN RIGHT(@StartingNodeAliasPath, 1) = ''%'' THEN IIF(LEFT(@StartingNodeAliasPath, 1) <> ''/'', ''/'', '''') + @StartingNodeAliasPath
	WHEN RIGHT(@StartingNodeAliasPath, 1) = ''/'' THEN IIF(LEFT(@StartingNodeAliasPath, 1) <> ''/'', ''/'', '''') + @StartingNodeAliasPath + ''%''
	ELSE IIF(LEFT(@StartingNodeAliasPath, 1) <> ''/'', ''/'', '''') + @StartingNodeAliasPath + ''/%''
END;


WITH '
-- NodesInTreeSection CTE provides the set of nodes in the current section so that we don't
-- have to use the NodeAliasPath LIKE pattern repeatedly.
	SET @sql = @sql
	+ N'
	NodesInTreeSection
		(
			NodeID,
			NodeAliasPath,
			AnyChildNodeAliasExpression,
			DocumentCulture,
			NodeClassID,
			NodeSiteID,
			NodeName,
			DocumentName,
			NodeGUID,
			NodeLevel,
			NodeOrder,
			NodeParentID
		) AS (
		SELECT
			NodeID,
			NodeAliasPath,
			CASE
				WHEN NodeAliasPath = ''/'' THEN NodeAliasPath + ''%''
				ELSE NodeAliasPath + ''/%''
			END As AnyChildNodeAliasExpression,
			DocumentCulture,
			NodeClassID,
			NodeSiteID,
			NodeName,
			DocumentName,
			NodeGUID,
			NodeLevel,
			NodeOrder,
			NodeParentID
		FROM
			View_CMS_Tree_Joined As Tree';

IF(@StartingNodeAliasPath = '' OR @StartingNodeAliasPath IS NULL)
BEGIN
		SET @sql = @sql
	+ N'
		WHERE
			NodeSiteID = @SiteID
	)';
END
ELSE
BEGIN
	SET @sql = @sql
	+ N'
		WHERE
			NodeSiteID = @SiteID
			AND
			(
				/* Is the root path */
				Tree.NodeAliasPath = @StartingNodeAliasPath
				OR
				/* Is under the root path */
				Tree.NodeAliasPath LIKE @StartingPathLikeExpression
			)
	)';
END

-- SelectableTreeNodes CTE provides set of selectable nodes.
-- First check if a list of page types was provided, by seeing
-- if the table-valued parameter has one placeholder row.
-- This hack is because we can't use a stored procedure with the 
-- UniTreeProvider.
IF ('' IN (SELECT TOP 1 Value FROM @SelectablePageTypes))
BEGIN
	-- There are no selectable types, so all types are selectable
	SET @sql = @sql
	+ N'
	,SelectableTreeNodes (NodeID, NodeAliasPath, AnyChildNodeAliasExpression) AS (
		SELECT
			NodeID,
			NodeAliasPath,
			AnyChildNodeAliasExpression
		FROM
			NodesInTreeSection As Tree
		WHERE
			DocumentCulture = @Culture
	)';
END
ELSE
BEGIN
-- There are selectable types, so use only the subset
-- of matching nodes.
	SET @sql = @sql
	+ N'
	,SelectableTreeNodes (NodeID, NodeAliasPath) AS (
		SELECT
			NodeID,
			NodeAliasPath
		FROM
			NodesInTreeSection As Tree
		WHERE
			NodeSiteID = @SiteID
			AND
			DocumentCulture = @Culture
			AND
			NodeID IN (SELECT NodeID FROM NodesInTreeSection)
			AND
			/* This node is a selectable type */
			Tree.NodeClassID IN (SELECT ClassID FROM CMS_CLass WHERE ClassName IN (SELECT * FROM @SelectablePageTypes))
	)';
END

-- Add a CTE that provides the set of nodes that are either selectable
-- or have selectable children
SET @sql = @sql
+ N'
,SelectableOrHasSelectableChildren (NodeID, NodeAliasPath) AS (
	SELECT
		NodeID,
		NodeAliasPath
	FROM
		NodesInTreeSection As Tree
	WHERE
		/* Current node is selectable */
		NodeID IN (Select NodeID FROM SelectableTreeNodes)
		OR
		/* It has a child node that is selectable */
		EXISTS
		(
			SELECT
				TT.NodeID
			FROM
				SelectableTreeNodes AS TT
			WHERE
				(TT.NodeAliasPath LIKE Tree.AnyChildNodeAliasExpression)
		)
)';

-- Add a CTE to provide the set of search hits. 
-- If there is no SearchText parameter value, this
-- will be a cheap, empty query.
IF (@SearchText = '' OR @SearchText IS NULL)
BEGIN
	SET @sql = @sql
	+ N'
	,SearchHits (NodeID, NodeAliasPath) AS (
		SELECT
			NodeID,
			NodeAliasPath
		FROM
			CMS_Tree
		WHERE
			0=1
	)';
END
ELSE
BEGIN
	SET @sql = @sql
	+ N'
	,SearchHits (NodeID, NodeAliasPath) AS (
	SELECT
		NodeID,
		NodeAliasPath
	FROM
		View_CMS_Tree_Joined As Tree
	WHERE
		NodeID IN (SELECT NodeID FROM SelectableTreeNodes)
		AND
		/* This node satisfies the searchtext */
		Tree.DocumentName LIKE @SearchLikeExpression
	)';
END
-- Now add the main SELECT expression that returns the
-- result set using the CTEs above.

SET @sql = @sql
+ N'
SELECT
    (
        SELECT
            COUNT(T.NodeID)
        FROM
            CMS_Tree AS T
        WHERE
            T.NodeParentID = Tree.NodeID
            AND
			T.NodeSiteID = @SiteID
    ) AS NodeChildCount,
    (
        SELECT
            COUNT(TT.NodeID)
        FROM
            NodesInTreeSection AS TT
        WHERE
            (TT.NodeGUID IN (select * from @SelectedNodeGuids))
            AND
			(TT.NodeAliasPath LIKE Tree.AnyChildNodeAliasExpression)
			AND
			TT.NodeID IN (SELECT NodeID FROM SelectableTreeNodes)
			AND
			TT.DocumentCulture = @Culture
    ) AS ChildChecked,
	CASE
		WHEN (NodeID IN (SELECT NodeID FROM SelectableTreeNodes)) THEN 1
		ELSE 0
	END As IsSelectable,
	CASE
		WHEN (Tree.DocumentCulture = @Culture) THEN 1
		ELSE 0
	END As IsCurrentCulture,
	CASE
		WHEN (NodeID IN (SELECT NodeID FROM SearchHits)) THEN 1
		ELSE 0
	END As IsSearchHit,
	(
        SELECT
            CASE
				WHEN COUNT(TT.NodeID) > 0 THEN 1
				ELSE 0
			END
        FROM
            NodesInTreeSection AS TT
        WHERE
			(TT.NodeAliasPath LIKE Tree.AnyChildNodeAliasExpression)
			AND
			TT.NodeID IN (SELECT NodeID FROM SearchHits)
    ) AS HasChildSearchHit,
    NodeName,
    DocumentName,
    NodeGUID,
    NodeID,
    NodeLevel,
    NodeOrder,
    NodeParentID,
    NodeAliasPath,
    ClassIconClass,
    Class.ClassName,
    DocumentCulture
FROM
    NodesInTreeSection As Tree
    INNER JOIN CMS_Class As Class ON Class.ClassID = Tree.NodeClassID
WHERE
	NodeID IN (SELECT NodeID FROM SelectableOrHasSelectableChildren)
	AND
	(
		/* In the current culture */
		DocumentCulture = @Culture
		OR
		/* Does NOT exist in the current culture
		   But has selectable children that are. */
		(
			Tree.NodeID NOT IN (SELECT TT.NodeID FROM View_CMS_Tree_Joined TT WHERE TT.DocumentCulture = @Culture)
			AND
			EXISTS
				(SELECT
					TT.NodeID
				FROM
					SelectableTreeNodes AS TT
				WHERE
					(TT.NodeAliasPath LIKE Tree.AnyChildNodeAliasExpression)
				)
		)
	)
';

-- Add the SearchHits condition if needed
IF(@SearchText <> '' AND @SearchText IS NOT NULL)
BEGIN
SET @sql = @sql
+ N'
	AND
	(	
		/* Is a search hit */
		NodeID IN (SELECT NodeID FROM SearchHits)
		OR
		/* Has a child that is a search hit */
		EXISTS
		(
			SELECT
				TT.NodeID
			FROM
				SearchHits AS TT
			WHERE
				(TT.NodeAliasPath LIKE Tree.AnyChildNodeAliasExpression)
		)
	)'
END
-- Add WHERE and ORDER BY clauses that are provided by the UniTreeProvider
-- The WHERE clause will provide conditions on NodeLevel and NodeParent
IF (N'##WHERE##' NOT LIKE N'##%##')
BEGIN
	SET @sql = @sql
	+ N'
	AND
	##WHERE##';
END
IF (N'##ORDERBY##' NOT LIKE N'##%##')
BEGIN
	SET @sql = @sql
	+ N'ORDER BY
    ##ORDERBY##';
END
/*
SELECT [Statement] = @sql
*/
DECLARE @paramaterDefinitions nvarchar(max);

SET @paramaterDefinitions = N'@SiteID int,
							  @Culture nvarchar(50),
							  @StartingNodeAliasPath nvarchar(450),
							  @SearchText nvarchar(50),
							  @SelectedNodeGuids [Type_CMS_GuidTable] READONLY,
							  @SelectablePageTypes [Type_CMS_StringTable] READONLY';
exec sp_executesql @sql,
				   @paramaterDefinitions,
				   @SiteID=@SiteID,
				   @Culture=@Culture,
				   @StartingNodeAliasPath=@StartingNodeAliasPath,
				   @SearchText=@SearchText,
				   @SelectedNodeGuids=@SelectedNodeGuids,
				   @SelectablePageTypes=@SelectablePageTypes;
";

    }
}
