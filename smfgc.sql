/*
SQLyog Ultimate v12.09 (64 bit)
MySQL - 5.7.28-log : Database - smfgc_db
*********************************************************************
*/

/*!40101 SET NAMES utf8 */;

/*!40101 SET SQL_MODE=''*/;

/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
CREATE DATABASE /*!32312 IF NOT EXISTS*/`smfgc_db` /*!40100 DEFAULT CHARACTER SET utf8 */;

USE `smfgc_db`;

/*Table structure for table `classroom_tb` */

DROP TABLE IF EXISTS `classroom_tb`;

CREATE TABLE `classroom_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `number` varchar(45) NOT NULL,
  `dept_id` int(10) NOT NULL,
  `dev_id` int(10) NOT NULL,
  `relay_1` int(1) NOT NULL,
  `relay_2` int(1) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Device ID` (`dev_id`),
  UNIQUE KEY `Classroom No.` (`number`),
  KEY `CR_Department` (`dept_id`),
  CONSTRAINT `CR_Department` FOREIGN KEY (`dept_id`) REFERENCES `department_tb` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `CR_Device` FOREIGN KEY (`dev_id`) REFERENCES `device_tb` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8;

/*Table structure for table `course_tb` */

DROP TABLE IF EXISTS `course_tb`;

CREATE TABLE `course_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `year` int(4) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8;

/*Table structure for table `department_tb` */

DROP TABLE IF EXISTS `department_tb`;

CREATE TABLE `department_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `floor` varchar(10) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8;

/*Table structure for table `device_tb` */

DROP TABLE IF EXISTS `device_tb`;

CREATE TABLE `device_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `serial_no` int(25) NOT NULL,
  `ip_addr` varchar(16) NOT NULL,
  `mac_addr` varchar(17) NOT NULL,
  `port` int(5) DEFAULT '2316',
  `rfid` tinyint(1) DEFAULT '1',
  `pzem` tinyint(1) DEFAULT '1',
  `status` tinyint(1) NOT NULL DEFAULT '0',
  `last_uidtag` varchar(12) DEFAULT NULL,
  `uptime` datetime DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `IP Address` (`ip_addr`),
  UNIQUE KEY `MAC Address` (`mac_addr`),
  UNIQUE KEY `Serial Number` (`serial_no`)
) ENGINE=InnoDB AUTO_INCREMENT=522 DEFAULT CHARSET=utf8;

/*Table structure for table `faculty_tb` */

DROP TABLE IF EXISTS `faculty_tb`;

CREATE TABLE `faculty_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `level` tinyint(1) NOT NULL DEFAULT '1',
  `uidtag` varchar(12) NOT NULL,
  `title` varchar(45) NOT NULL,
  `last_name` varchar(45) NOT NULL,
  `first_name` varchar(45) NOT NULL,
  `mi` varchar(45) NOT NULL,
  `picture` mediumblob,
  `sfv_count` int(5) NOT NULL DEFAULT '0',
  `sfv_time` time NOT NULL DEFAULT '01:00:00',
  `sfv_limit` int(5) NOT NULL DEFAULT '10',
  PRIMARY KEY (`id`),
  UNIQUE KEY `UIDTAG` (`uidtag`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8;

/*Table structure for table `pzem_tb` */

DROP TABLE IF EXISTS `pzem_tb`;

CREATE TABLE `pzem_tb` (
  `dev_id` int(8) NOT NULL,
  `volt` double(4,1) NOT NULL,
  `current` double(4,2) NOT NULL,
  `power` double(4,1) NOT NULL,
  `energy` double(8,4) NOT NULL,
  `frequency` double(4,2) NOT NULL,
  `pf` double(4,2) NOT NULL,
  `ts` timestamp(3) NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  KEY `PZEM_DeviceID` (`dev_id`),
  CONSTRAINT `PZ_Device` FOREIGN KEY (`dev_id`) REFERENCES `device_tb` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*Table structure for table `schedule_tb` */

DROP TABLE IF EXISTS `schedule_tb`;

CREATE TABLE `schedule_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `course_id` int(10) NOT NULL,
  `subject_id` int(10) NOT NULL,
  `room_id` int(10) NOT NULL,
  `faculty_id` int(10) NOT NULL,
  `day` varchar(12) DEFAULT 'Sunday',
  `start_time` time NOT NULL,
  `end_time` time NOT NULL,
  PRIMARY KEY (`id`),
  KEY `CS_CourseID` (`course_id`),
  KEY `CS_SubjectID` (`subject_id`),
  KEY `CS_RoomID` (`room_id`),
  KEY `CS_FacultyID` (`faculty_id`),
  CONSTRAINT `CS_CourseID` FOREIGN KEY (`course_id`) REFERENCES `course_tb` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `CS_Faculty` FOREIGN KEY (`faculty_id`) REFERENCES `faculty_tb` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `CS_RoomID` FOREIGN KEY (`room_id`) REFERENCES `classroom_tb` (`id`) ON UPDATE CASCADE,
  CONSTRAINT `CS_SubjectID` FOREIGN KEY (`subject_id`) REFERENCES `subject_tb` (`id`) ON UPDATE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=12 DEFAULT CHARSET=utf8;

/*Table structure for table `subject_tb` */

DROP TABLE IF EXISTS `subject_tb`;

CREATE TABLE `subject_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `code` varchar(45) NOT NULL,
  `desc` varchar(255) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8;

/*Table structure for table `syslog_tb` */

DROP TABLE IF EXISTS `syslog_tb`;

CREATE TABLE `syslog_tb` (
  `ts` timestamp(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  `process` varchar(25) NOT NULL,
  `alert` int(3) NOT NULL,
  `message` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*Table structure for table `user_tb` */

DROP TABLE IF EXISTS `user_tb`;

CREATE TABLE `user_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `username` varchar(45) NOT NULL,
  `password` varchar(45) NOT NULL,
  `fullname` varchar(45) NOT NULL,
  `dept_id` int(10) DEFAULT '0',
  `role` varchar(8) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8;

/* Function  structure for function  `UC_Words` */

/*!50003 DROP FUNCTION IF EXISTS `UC_Words` */;
DELIMITER $$

/*!50003 CREATE DEFINER=`root`@`localhost` FUNCTION `UC_Words`(str VARCHAR (255)) RETURNS varchar(255) CHARSET utf8
    DETERMINISTIC
BEGIN
  DECLARE c CHAR(1) ;
  DECLARE s VARCHAR (255) ;
  DECLARE i INT DEFAULT 1 ;
  DECLARE bool INT DEFAULT 1 ;
  DECLARE punct CHAR(17) DEFAULT ' ()[]{},.-_!@;:?/' ;
  SET s = LCASE(str) ;
  WHILE
    i < LENGTH(str) DO 
    BEGIN
      SET c = SUBSTRING(s, i, 1) ;
      IF LOCATE(c, punct) > 0 
      THEN SET bool = 1 ;
      ELSEIF bool = 1 
      THEN 
      BEGIN
        IF c >= 'a' 
        AND c <= 'z' 
        THEN 
        BEGIN
          SET s = CONCAT(LEFT(s, i - 1), UCASE(c), SUBSTRING(s, i + 1)) ;
          SET bool = 0 ;
        END ;
        ELSEIF c >= '0' 
        AND c <= '9' 
        THEN SET bool = 0 ;
        END IF ;
      END ;
      END IF ;
      SET i = i + 1 ;
    END ;
  END WHILE ;
  RETURN s ;
END */$$
DELIMITER ;

/*Table structure for table `classroom_v` */

DROP TABLE IF EXISTS `classroom_v`;

/*!50001 DROP VIEW IF EXISTS `classroom_v` */;
/*!50001 DROP TABLE IF EXISTS `classroom_v` */;

/*!50001 CREATE TABLE  `classroom_v`(
 `ID` int(10) ,
 `Name` varchar(45) ,
 `Number` varchar(45) ,
 `Deptparment` varchar(45) ,
 `Device S/N` int(25) ,
 `Fan/Lights` varchar(3) ,
 `Outlet` varchar(3) 
)*/;

/*Table structure for table `pzem_v` */

DROP TABLE IF EXISTS `pzem_v`;

/*!50001 DROP VIEW IF EXISTS `pzem_v` */;
/*!50001 DROP TABLE IF EXISTS `pzem_v` */;

/*!50001 CREATE TABLE  `pzem_v`(
 `dev_id` int(8) ,
 `volt_min` double(18,1) ,
 `volt_max` double(18,1) ,
 `volt_avg` varchar(43) ,
 `volt` double(4,1) ,
 `current_min` double(19,2) ,
 `current_max` double(19,2) ,
 `current_avg` varchar(43) ,
 `current` double(4,2) ,
 `power_min` double(18,1) ,
 `power_max` double(18,1) ,
 `power_avg` varchar(43) ,
 `power` double(4,1) ,
 `energy_min` double(21,4) ,
 `energy_max` double(21,4) ,
 `energy_avg` varchar(49) ,
 `energy` double(8,4) ,
 `frequency_min` double(19,2) ,
 `frequency_max` double(19,2) ,
 `frequency_avg` varchar(43) ,
 `frequency` double(4,2) ,
 `pf_min` double(19,2) ,
 `pf_max` double(19,2) ,
 `pf_avg` varchar(43) ,
 `pf` double(4,2) 
)*/;

/*Table structure for table `schedule_v` */

DROP TABLE IF EXISTS `schedule_v`;

/*!50001 DROP VIEW IF EXISTS `schedule_v` */;
/*!50001 DROP TABLE IF EXISTS `schedule_v` */;

/*!50001 CREATE TABLE  `schedule_v`(
 `ID` int(10) ,
 `Course` varchar(59) ,
 `Subject Code` varchar(45) ,
 `Day` varchar(12) ,
 `Start Time` varchar(8) ,
 `End Time` varchar(8) ,
 `Room` varchar(91) ,
 `Faculty` varchar(183) 
)*/;

/*View structure for view classroom_v */

/*!50001 DROP TABLE IF EXISTS `classroom_v` */;
/*!50001 DROP VIEW IF EXISTS `classroom_v` */;

/*!50001 CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `classroom_v` AS (select `ct`.`id` AS `ID`,`ct`.`name` AS `Name`,`ct`.`number` AS `Number`,`dt`.`name` AS `Deptparment`,`dv`.`serial_no` AS `Device S/N`,if((`ct`.`relay_1` = 1),'Yes','NO') AS `Fan/Lights`,if((`ct`.`relay_2` = 1),'Yes','NO') AS `Outlet` from ((`classroom_tb` `ct` join `department_tb` `dt` on((`dt`.`id` = `ct`.`dept_id`))) join `device_tb` `dv` on((`dv`.`id` = `ct`.`dev_id`))) limit 100) */;

/*View structure for view pzem_v */

/*!50001 DROP TABLE IF EXISTS `pzem_v` */;
/*!50001 DROP VIEW IF EXISTS `pzem_v` */;

/*!50001 CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `pzem_v` AS (select `pzem_tb`.`dev_id` AS `dev_id`,min(nullif(`pzem_tb`.`volt`,0)) AS `volt_min`,max(nullif(`pzem_tb`.`volt`,0)) AS `volt_max`,format(avg(`pzem_tb`.`volt`),2) AS `volt_avg`,`pzem_tb`.`volt` AS `volt`,min(nullif(`pzem_tb`.`current`,0)) AS `current_min`,max(nullif(`pzem_tb`.`current`,0)) AS `current_max`,format(avg(`pzem_tb`.`current`),2) AS `current_avg`,`pzem_tb`.`current` AS `current`,min(nullif(`pzem_tb`.`power`,0)) AS `power_min`,max(nullif(`pzem_tb`.`power`,0)) AS `power_max`,format(avg(`pzem_tb`.`power`),1) AS `power_avg`,`pzem_tb`.`power` AS `power`,min(nullif(`pzem_tb`.`energy`,0)) AS `energy_min`,max(nullif(`pzem_tb`.`energy`,0)) AS `energy_max`,format(avg(`pzem_tb`.`energy`),4) AS `energy_avg`,`pzem_tb`.`energy` AS `energy`,min(nullif(`pzem_tb`.`frequency`,0)) AS `frequency_min`,max(nullif(`pzem_tb`.`frequency`,0)) AS `frequency_max`,format(avg(`pzem_tb`.`frequency`),2) AS `frequency_avg`,`pzem_tb`.`frequency` AS `frequency`,min(nullif(`pzem_tb`.`pf`,0)) AS `pf_min`,max(nullif(`pzem_tb`.`pf`,0)) AS `pf_max`,format(avg(`pzem_tb`.`pf`),2) AS `pf_avg`,`pzem_tb`.`pf` AS `pf` from `pzem_tb` group by `pzem_tb`.`dev_id` desc) */;

/*View structure for view schedule_v */

/*!50001 DROP TABLE IF EXISTS `schedule_v` */;
/*!50001 DROP VIEW IF EXISTS `schedule_v` */;

/*!50001 CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `schedule_v` AS (select `st`.`id` AS `ID`,concat(`cu`.`name`,' - ',`cu`.`year`) AS `Course`,`sj`.`code` AS `Subject Code`,`st`.`day` AS `Day`,time_format(`st`.`start_time`,'%h:%i %p') AS `Start Time`,time_format(`st`.`end_time`,'%h:%i %p') AS `End Time`,concat(`cr`.`name`,' ',`cr`.`number`) AS `Room`,concat(`ft`.`title`,' ',`ft`.`last_name`,' ',`ft`.`first_name`,' ',`ft`.`mi`) AS `Faculty` from ((((`schedule_tb` `st` join `course_tb` `cu` on((`cu`.`id` = `st`.`course_id`))) join `subject_tb` `sj` on((`sj`.`id` = `st`.`subject_id`))) join `classroom_tb` `cr` on((`cr`.`id` = `st`.`room_id`))) join `faculty_tb` `ft` on((`ft`.`id` = `st`.`faculty_id`))) limit 100) */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
