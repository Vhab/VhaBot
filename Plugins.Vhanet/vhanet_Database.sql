CREATE TABLE `alts` (
  `altname` varchar(14) NOT NULL,
  `altID` bigint(11) NOT NULL,
  `username` varchar(14) NOT NULL,
  `addedBy` varchar(14) NOT NULL default 'System',
  `addedOn` int(11) NOT NULL default '0',
  `online` int(1) NOT NULL default '0',
  UNIQUE KEY `altname` (`altname`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `bots` (
  `bot` varchar(14) NOT NULL,
  `group` varchar(14) NOT NULL,
  `raid` tinyint(1) NOT NULL default '0',
  `tracker` tinyint(1) NOT NULL default '0',
  `requirementMain` int(3) NOT NULL default '0',
  `requirementAlt` int(3) NOT NULL default '0',
  `allowedClan` tinyint(1) NOT NULL default '0',
  `allowedNeutral` tinyint(1) NOT NULL default '0',
  `allowedOmni` tinyint(1) NOT NULL default '0',
  `autoEnable` tinyint(1) NOT NULL default '0',
  `description` varchar(255) NOT NULL,
  UNIQUE KEY `bot` (`bot`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `members` (
  `username` varchar(14) NOT NULL,
  `userID` bigint(11) NOT NULL,
  `addedBy` varchar(14) NOT NULL,
  `addedOn` int(11) NOT NULL,
  `password` varchar(32) default NULL,
  `online` int(1) NOT NULL default '0',
  UNIQUE KEY `username` (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `members_access` (
  `username` varchar(14) NOT NULL default '',
  `group` varchar(50) NOT NULL default '',
  UNIQUE KEY `username` (`username`,`group`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `members_levels` (
  `username` varchar(14) NOT NULL default '',
  `group` varchar(50) NOT NULL default '',
  `userLevel` int(11) NOT NULL default '0',
  UNIQUE KEY `username` (`username`,`group`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `tracker_members` (
  `Nickname` varchar(14) NOT NULL,
  `Level` int(3) default NULL,
  `AlienLevel` int(2) default NULL,
  `Profession` varchar(30) default NULL,
  `Org` varchar(255) default NULL,
  `OrgID` int(11) default NULL,
  `Faction` varchar(8) default NULL,
  `Online` int(1) default NULL,
  `Logon` int(11) default NULL,
  `LastUpdated` int(11) default NULL,
  PRIMARY KEY  (`Nickname`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;

CREATE TABLE `tracker_orgs` (
  `ID` int(11) NOT NULL,
  `OrgName` varchar(255) default NULL,
  `Faction` varchar(10) default NULL,
  `LastUpdated` int(11) default NULL,
  PRIMARY KEY  (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=latin1;