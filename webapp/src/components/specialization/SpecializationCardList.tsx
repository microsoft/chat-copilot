import React, { useId } from 'react';
import Carousel from 'react-multi-carousel';
import 'react-multi-carousel/lib/styles.css';
import { ISpecialization } from '../../libs/models/Specialization';
import { SpecializationCard } from './SpecializationCard';
import { useAppSelector } from '../../redux/app/hooks';
import { RootState } from '../../redux/app/store';
import { makeStyles } from '@fluentui/react-components';

const responsive = {
    // Define responsive settings for different screen sizes
    desktop: {
        breakpoint: { max: 3000, min: 1024 },
        items: 2,
        slidesToSlide: 2,
    },
    tablet: {
        breakpoint: { max: 1024, min: 464 },
        items: 2,
        slidesToSlide: 2,
    },
    mobile: {
        breakpoint: { max: 464, min: 0 },
        items: 1,
        slidesToSlide: 1,
    },
};

const useClasses = makeStyles({
    innercontainerclass: {
        height: '330px',
        position: 'relative',
        display: 'block',
    },
    innertitle: {
        textAlign: 'center',
    },
    carouselroot: {
        position: 'relative',
        //marginRight: 'calc(100% - 1024px)',
        display: 'block',
        width: '700px',
    },
});

interface SpecializationProps {
    /* eslint-disable 
      @typescript-eslint/no-unsafe-assignment
    */
    specializations: ISpecialization[];
}

export const SpecializationCardList: React.FC<SpecializationProps> = ({ specializations }) => {
    const specializaionCarouselId = useId();
    const specializaionCardId = useId();
    const classes = useClasses();
    const { app } = useAppSelector((state: RootState) => state);
    const filteredSpecializations = specializations.filter((_specialization) => {
        const hasMembership = app.activeUserInfo?.groups.some((val) => {
            return _specialization.groupMemberships.includes(val);
        });
        // eslint-disable-next-line @typescript-eslint/prefer-nullish-coalescing
        if ((hasMembership && _specialization.isActive) || _specialization.key == 'general') {
            return _specialization;
        }
        return;
    });
    return (
        <div className={classes.carouselroot}>
            <h1 className={classes.innertitle}>Choose Specialization</h1>
            <Carousel
                responsive={responsive}
                key={specializaionCarouselId}
                showDots={true}
                swipeable={true}
                arrows={true}
                dotListClass="custom-dot-list-style"
            >
                {filteredSpecializations.map((_specialization, index) => (
                    <div key={index} className={classes.innercontainerclass}>
                        <SpecializationCard
                            key={specializaionCardId + '_' + index.toString()}
                            specialization={_specialization}
                        />
                    </div>
                ))}
            </Carousel>
        </div>
    );
};
