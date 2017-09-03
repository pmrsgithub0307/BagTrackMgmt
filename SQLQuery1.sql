select * from OSUSR_UUK_FLT_INFO;
select * from OSUSR_UUK_H2H;
select * from OSUSR_UUK_BAGMSGS;
select * from OSUSR_UUK_BAGINTEG;
select IFLIGHT, FFLTNR, count(distinct number) from OSUSR_UUK_BAGINTEG group by IFLIGHT, FFLTNR;