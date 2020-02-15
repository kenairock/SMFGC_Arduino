﻿using MySql.Data.MySqlClient;
using System;

namespace SMFGC {
    class pVariables {

        // If you change the row names you also need to 
        // change the code by going to definition

        public static int server_port = 2316;

        public static bool AdminMode = false, DeptMode = false;

        public static bool confirmExit = false;

        public static readonly String Project_Name = "Safety and Security System";

        public static readonly String sConn = "datasource=localhost;port=3306;database=smfgc_db;username=smfgc;password=P@ssw0rd;";

        public static readonly String qLogin = @"SELECT `fullname`,`role` FROM `user_tb` WHERE `username` = @user AND `password` = @pass LIMIT 1;";

        public static String qRooms = @"SELECT `id`,`name`,`number` FROM `classroom_tb`";

        public static readonly String qDeviceCheck = @"SELECT `dt`.`last_uidtag`,
                                                            `ct`.`id`,
                                                            CONCAT(`ct`.`name`, ' ', `ct`.`number`) AS `name`,
                                                            `ct`.`relay_1`,`ct`.`relay_2` 
                                                        FROM
                                                            `device_tb` `dt` 
                                                            JOIN `classroom_tb` `ct` ON `dt`.`id` = `ct`.`dev_id` 
                                                        WHERE `dt`.`id` = @p1 AND `dt`.`mac_addr` = @p2 LIMIT 1 ;";

        public static readonly String qUidTagCheck = @"SELECT `ft`.`id`, CONCAT(`ft`.`title`,' ',`ft`.`last_name`,' ',`ft`.`first_name`,' ',`ft`.`mi`) AS `faculty`,
                                                          `ft`.`level`,`ft`.`sfv_count`,`ft`.`sfv_limit`,`ft`.`sfv_time` 
                                                        FROM `faculty_tb` `ft` WHERE `uidtag` = @p1 LIMIT 1;";

        public static readonly String qCheckSched = @"SELECT `st`.`id`, `st`.`end_time` FROM `schedule_tb` st 
                                                      WHERE `room_id` = @p1 
                                                          AND (`start_time` < NOW() AND `end_time` > NOW()) 
                                                          AND `day` = DAYNAME(NOW()) LIMIT 1;";

        public static readonly String qUpdateDevPing = @"UPDATE `device_tb` SET `status`=@p1, `last_uidtag`= IF((@p2 = NULL), `last_uidtag`, @p2),`uptime`= IF((@p1 = 2), NOW(), `uptime`) WHERE `id`=@p3;";

        public static readonly String qUpdateDevPing_IP = @"UPDATE LOW_PRIORITY `device_tb` SET `status` = IF(((@p1 < `status`) AND ( @p1 = 1 )), `status`, @p1) WHERE `ip_addr`=@p2;";

        public static readonly String qDevices = @"SELECT `ip_addr` FROM `device_tb`;";

        public static readonly String qLogger = @"INSERT INTO `syslog_tb` (`process`, `alert`, `message`) VALUES (@p1, @p2, @p3);";

        public static readonly String qPZEMLog = @"INSERT INTO `pzem_tb` (`dev_id`, `volt`, `current`, `power`, `energy`, `frequency`, `pf`) VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7);";

        public static readonly String qDevPingCheck = @"SELECT `id` FROM `device_tb` WHERE `id`=@p1 AND (`status` > 0);";
        
        public static readonly String qLogReport = @"SELECT DATE_FORMAT(`ts`, '%b %d, %Y - %r') AS `Date/Time Logged`,
                                                      IF(`alert` = 64,'Information',
                                                        IF(`alert` = 48,'Warning',
                                                          IF(`alert` = 32,'Question',
                                                            IF(`alert` = 16, 'Error', '-')))) AS `Alert`,
                                                      `message` AS `Message` 
                                                    FROM `syslog_tb` WHERE `process` = @p1 ORDER BY `ts` DESC LIMIT 100;";

        public static readonly String qFacultSFV = @"UPDATE `faculty_tb` SET `sfv_count`= `sfv_count` + @p1 WHERE `id`=@p2;";
    }
}
