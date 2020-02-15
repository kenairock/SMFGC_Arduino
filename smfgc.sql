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

/*Data for the table `classroom_tb` */

insert  into `classroom_tb`(`id`,`name`,`number`,`dept_id`,`dev_id`,`relay_1`,`relay_2`) values (5,'ROOM','514',1,514,1,1),(10,'ROOM','515',1,515,1,0);

/*Table structure for table `course_tb` */

DROP TABLE IF EXISTS `course_tb`;

CREATE TABLE `course_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `year` int(4) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8;

/*Data for the table `course_tb` */

insert  into `course_tb`(`id`,`name`,`year`) values (1,'BSCpE',2020),(2,'BSIT',2020),(3,'BSBA',2020),(4,'BSCrim',2020),(5,'BLIS',2020);

/*Table structure for table `department_tb` */

DROP TABLE IF EXISTS `department_tb`;

CREATE TABLE `department_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `name` varchar(45) NOT NULL,
  `floor` varchar(10) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Name` (`name`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8;

/*Data for the table `department_tb` */

insert  into `department_tb`(`id`,`name`,`floor`) values (1,'CASE','2'),(6,'IT','3'),(7,'HRM','G'),(8,'CPE','4');

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
) ENGINE=InnoDB AUTO_INCREMENT=519 DEFAULT CHARSET=utf8;

/*Data for the table `device_tb` */

insert  into `device_tb`(`id`,`serial_no`,`ip_addr`,`mac_addr`,`port`,`rfid`,`pzem`,`status`,`last_uidtag`,`uptime`) values (514,415051,'192.168.1.51','DE-AD-BE-EF-FE-E0',2316,1,1,0,NULL,NULL),(515,515052,'192.168.1.52','DE-AD-BE-EF-FE-E1',2316,1,1,0,NULL,NULL),(518,556895,'10.0.1.100','DE-AD-BE-EF-FE-E2',2316,1,1,1,NULL,NULL);

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

/*Data for the table `faculty_tb` */

insert  into `faculty_tb`(`id`,`level`,`uidtag`,`title`,`last_name`,`first_name`,`mi`,`picture`,`sfv_count`,`sfv_time`,`sfv_limit`) values (1,1,'e563c223','Doc.','Miguel','Jolly','A.',NULL,1,'01:00:00',10);

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

/*Data for the table `pzem_tb` */

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

/*Data for the table `schedule_tb` */

insert  into `schedule_tb`(`id`,`course_id`,`subject_id`,`room_id`,`faculty_id`,`day`,`start_time`,`end_time`) values (11,1,10,5,1,'Saturday','12:00:00','15:00:00');

/*Table structure for table `subject_tb` */

DROP TABLE IF EXISTS `subject_tb`;

CREATE TABLE `subject_tb` (
  `id` int(10) NOT NULL AUTO_INCREMENT,
  `code` varchar(45) NOT NULL,
  `desc` varchar(255) NOT NULL,
  PRIMARY KEY (`id`),
  UNIQUE KEY `Code` (`code`)
) ENGINE=InnoDB AUTO_INCREMENT=18 DEFAULT CHARSET=utf8;

/*Data for the table `subject_tb` */

insert  into `subject_tb`(`id`,`code`,`desc`) values (5,'ELEC 1','IT Productivity Tools'),(6,'MT 100','College Algebra'),(7,'PE 1','Physical Fitness and Gymnastics'),(8,'NS 100','Ecology'),(9,'EN 120','Speech Communication'),(10,'CPE 440','Software Engineering'),(11,'HU 120','Introduction to Humanities'),(12,'IT 202','Operating Systems'),(13,'PE 4','Team Sports'),(14,'NSTP 1','Civic Welfare Training Service 1'),(15,'PE 2','Rhythmic Activities'),(16,'PE 3','Individual & Dual Sports'),(17,'ST 100','Statistics and Probability');

/*Table structure for table `syslog_tb` */

DROP TABLE IF EXISTS `syslog_tb`;

CREATE TABLE `syslog_tb` (
  `ts` timestamp(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  `process` varchar(25) NOT NULL,
  `alert` int(3) NOT NULL,
  `message` text NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

/*Data for the table `syslog_tb` */

insert  into `syslog_tb`(`ts`,`process`,`alert`,`message`) values ('2020-02-14 22:57:15.812','sys',64,'Server started.'),('2020-02-14 23:00:21.242','sys',64,'Server started.'),('2020-02-14 23:30:16.585','sys',64,'Server started.'),('2020-02-15 15:11:29.649','sys',64,'Server started.'),('2020-02-15 15:12:02.105','sys',64,'Server closed.'),('2020-02-15 15:13:47.119','sys',64,'Server started.'),('2020-02-15 15:13:49.244','sys',64,'Server closed.'),('2020-02-15 15:13:54.076','sys',64,'Server started.'),('2020-02-15 15:14:47.159','sys',64,'Server started.'),('2020-02-15 15:14:49.481','sys',64,'Server closed.'),('2020-02-15 15:15:28.207','sys',64,'Server started.'),('2020-02-15 15:15:33.927','sys',64,'Server closed.'),('2020-02-15 15:19:27.640','sys',64,'Server started.'),('2020-02-15 15:20:12.713','sys',64,'Server started.'),('2020-02-15 15:24:46.338','sys',64,'Server started.'),('2020-02-15 15:26:20.982','sys',64,'Server started.'),('2020-02-15 15:26:45.061','sys',64,'Server started.'),('2020-02-15 15:27:11.475','sys',64,'Server started.'),('2020-02-15 15:29:34.685','sys',64,'Server started.'),('2020-02-15 15:30:01.436','sys',64,'Server started.'),('2020-02-15 15:31:18.924','sys',64,'Server started.'),('2020-02-15 15:32:57.683','sys',64,'Server started.'),('2020-02-15 15:34:20.246','sys',64,'Server started.'),('2020-02-15 15:34:49.270','sys',64,'Server started.'),('2020-02-15 15:35:13.169','sys',64,'Server started.'),('2020-02-15 15:35:43.684','sys',64,'Server started.'),('2020-02-15 17:55:03.283','sys',64,'Server started.'),('2020-02-15 18:00:59.351','sys',64,'Server started.'),('2020-02-15 18:01:49.455','sys',64,'Server started.'),('2020-02-15 18:02:15.189','sys',64,'Server started.'),('2020-02-15 18:04:02.194','sys',64,'Server started.'),('2020-02-15 18:05:05.733','sys',64,'Server started.'),('2020-02-15 18:06:00.487','sys',64,'Server started.');

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

/*Data for the table `user_tb` */

insert  into `user_tb`(`id`,`username`,`password`,`fullname`,`dept_id`,`role`) values (5,'francis','admin','Francis Galvez',0,'Admin'),(6,'james','123','James Bond',1,'Dean');

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

/*View structure for view schedule_v */

/*!50001 DROP TABLE IF EXISTS `schedule_v` */;
/*!50001 DROP VIEW IF EXISTS `schedule_v` */;

/*!50001 CREATE ALGORITHM=UNDEFINED DEFINER=`root`@`localhost` SQL SECURITY DEFINER VIEW `schedule_v` AS (select `st`.`id` AS `ID`,concat(`cu`.`name`,' - ',`cu`.`year`) AS `Course`,`sj`.`code` AS `Subject Code`,`st`.`day` AS `Day`,time_format(`st`.`start_time`,'%h:%i %p') AS `Start Time`,time_format(`st`.`end_time`,'%h:%i %p') AS `End Time`,concat(`cr`.`name`,' ',`cr`.`number`) AS `Room`,concat(`ft`.`title`,' ',`ft`.`last_name`,' ',`ft`.`first_name`,' ',`ft`.`mi`) AS `Faculty` from ((((`schedule_tb` `st` join `course_tb` `cu` on((`cu`.`id` = `st`.`course_id`))) join `subject_tb` `sj` on((`sj`.`id` = `st`.`subject_id`))) join `classroom_tb` `cr` on((`cr`.`id` = `st`.`room_id`))) join `faculty_tb` `ft` on((`ft`.`id` = `st`.`faculty_id`))) limit 100) */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
