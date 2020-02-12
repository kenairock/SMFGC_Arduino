using MySql.Data.MySqlClient;
using System;

namespace SMFGC {
    class pVariables {

        // If you change the row names you also need to 
        // change the code by going to definition

        public static int server_port = 2316;

        public static bool AdminMode = false, DeptMode = false;

        public static readonly String Project_Name = "Safety and Security System";

        public static readonly String sConn = "datasource=localhost;port=3306;database=smfgc;username=smfgc;password=P@ssw0rd;";

        public static readonly String qLogin = @"SELECT `role` FROM `login_tb` WHERE `username` = @user AND `password` = @pass LIMIT 1;";

        public static readonly String qDeviceCheck = @"SELECT `room_id`,`classroom`,`relay1`,`relay2`,`status` 
                                                            FROM `classroom_tb` 
                                                        WHERE `dev_id` = @p1 AND `ip_add` = @p2 LIMIT 1;";

        public static readonly String qUidTagCheck = @"SELECT `users_id` FROM `users_tb` WHERE `uid` = @p1 LIMIT 1;";

        public static readonly String qCheckSched = @"SELECT `ct`.`sched_id`, `ct`.`end_time`, CONCAT(`ut`.`title`,' ',`ut`.`last_name`,' ',`ut`.`first_name`,' ',`ut`.`m_i`) AS `faculty` 
                                                            FROM `class_sched_tb` ct
                                                                JOIN `users_tb` `ut`
                                                                    ON `ut`.`users_id` = `ct`.`faculty`
                                                            WHERE `room_id` = @p1 
                                                                AND (`start_time` < NOW() AND `end_time` > NOW()) 
                                                                AND `day` = DAYNAME(NOW()) LIMIT 1;";

        public static readonly String qRoomUpdateSingle = @"UPDATE `classroom_tb` SET `status`=@p1, `uptime`= IF(@p1 = 1, NOW(), `uptime`) WHERE `room_id`=@p2;";

        public static readonly String qRoomPing = @"SELECT `ip_add` FROM `classroom_tb`;";
        public static readonly String qRoomPingUpdateStatus = @"UPDATE LOW_PRIORITY `classroom_tb` SET `status` = IF (((@p1 < `status`) AND ( @p1 = 1 )), `status`, @p1) WHERE `ip_add`=@p2;";

        public static readonly String qLogger = @"INSERT INTO `logs_tb` (`dev_id`, `uid`, `process`, `alert`, `message`) VALUES (@p1, @p2, @p3, @p4, @p5);";

        public static readonly String qPZEMLog = @"INSERT INTO `pzem_tb` (`dev_id`, `volt`, `current`, `power`, `energy`, `frequency`, `pf`) VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7);";

        public static readonly String qTCPBrokenCheck = @"SELECT `room_id` FROM `classroom_tb` WHERE `dev_id` =@p1 AND `status` > 0 LIMIT 1;";

        public static readonly String qRoomUpdateUID = "UPDATE `classroom_tb` SET `last_uidtag`=@p1, `status`=@p2 WHERE `room_id`=@p3;";

        public static readonly String qGetRoomUID = "SELECT `room_id` FROM `classroom_tb` WHERE `dev_id`=@p1 AND `last_uidtag`=@p2;";

        public static readonly String qLogReport = @"SELECT DATE_FORMAT(`tstamp`, '%b %d, %Y - %r') AS `Date/Time Logged`,
                                                      IF(`dev_id` = 0, '-', `dev_id`) AS `Device ID`,
                                                      IF(`uid` = 0, '-', `uid`) AS `UID Tag`,
                                                      IF(`alert` = 64,'Information',
                                                        IF(`alert` = 48,'Warning',
                                                          IF(`alert` = 32,'Question',
                                                            IF(`alert` = 16, 'Error', '-')
                                                            )
                                                          )
                                                        ) AS `Alert`,
                                                      `message` AS `Message` 
                                                    FROM
                                                      logs_tb
                                                    WHERE `process` = @p1 ORDER BY `tstamp` DESC LIMIT 100;";
    }
}
