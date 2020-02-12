﻿using System;

namespace SMFGC {
    class pVariables {

        // If you change the row names you also need to 
        // change the code by going to definition

        public static int server_port = 2316;

        public static bool AdminMode = false, DeptMode = false;

        public static readonly String Project_Name = "Safety and Security System";

        public static readonly String sConn = "datasource=localhost;port=3306;database=smfgc;username=smfgc;password=P@ssw0rd;";

        public static readonly String qLogin = @"SELECT `role` FROM `login_tb` WHERE `username` = @user AND `password` = @pass LIMIT 1;";

        public static readonly String qDeviceCheck = @"SELECT `room_id`,`classroom`,`relay1`,`relay2` 
                                                            FROM `classroom_tb` 
                                                        WHERE `dev_id` = @p1 AND `ip_add` = @p2 LIMIT 1;";

        public static readonly String qUidTagCheck = @"SELECT `users_id` FROM `users_tb` WHERE `uid` = @p1 LIMIT 1;";

        public static readonly String qCheckSched = @"SELECT `sched_id`, CONCAT(`ut`.`title`,' ',`ut`.`last_name`,' ',`ut`.`first_name`,' ',`ut`.`m_i`) AS `faculty` 
                                                            FROM `class_sched_tb` ct
                                                                JOIN `users_tb` `ut`
                                                                    ON `ut`.`users_id` = `ct`.`faculty`
                                                            WHERE `room_id` = @p1 
                                                                AND (`start_time` < NOW() AND `end_time` > NOW()) 
                                                                AND `day` = DAYNAME(NOW()) LIMIT 1;";

        public static readonly String qRoomUpdateSingle = @"UPDATE `classroom_tb` SET `status`=@p1, `uptime`= IF(@p1 = 1, NOW(), `uptime`) WHERE `room_id`=@p2;";

        public static readonly String qRoomPing = @"SELECT `ip_add` FROM `classroom_tb` WHERE `status` IN(0, 1);";
        public static readonly String qRoomUpdateStatus = @"UPDATE LOW_PRIORITY `classroom_tb` SET `status` =@p1 WHERE `ip_add` =@p2 AND `status` IN(0, 1);";

        public static readonly String qLogger = @"INSERT INTO `logs_tb` (`dev_id`, `uid`, `process`, `message`) VALUES (@p1, @p2, @p3, @p4);";

        public static readonly String qPZEMLog = @"INSERT INTO `pzem_tb` (`dev_id`, `volt`, `current`, `power`, `energy`, `frequency`, `pf`) VALUES (@p1, @p2, @p3, @p4, @p5, @p6, @p7);";

        public static readonly String qTCPBrokenCheck = @"SELECT `room_id` FROM `classroom_tb` WHERE `dev_id` =@p1 AND `status` > 0 LIMIT 1;";

        public static readonly String qRoomUpdateUID = "UPDATE `classroom_tb` SET `last_uidtag`=@p1 WHERE `room_id`=@p2;";

        public static readonly String qGetRoomUID = "SELECT `room_id` FROM `classroom_tb` WHERE `last_uidtag`=@p1;";
    }
}
